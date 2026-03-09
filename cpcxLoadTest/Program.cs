using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NBomber.Contracts;
using NBomber.CSharp;

namespace cpcxLoadTest;

record UserCredentials(string Username, string Password);
record UserSession(string Username, HttpClient Client);

record SendPostcardResponse(
    [property: JsonPropertyName("postcardId")] string PostcardId,
    [property: JsonPropertyName("receiverUsername")] string ReceiverUsername);

record PostcardStatsResponse(
    [property: JsonPropertyName("postcardsSent")] int PostcardsSent,
    [property: JsonPropertyName("postcardsReceived")] int PostcardsReceived);

class Program
{
    // --- Configuration ---
    const string BaseUrl = "https://localhost:44348";

    static readonly UserCredentials[] Users =
        Enumerable.Range(1, 200).Select(i => new UserCredentials($"user{i:D3}", "devpassword")).ToArray();

    // Shared queue: username -> numeric postcard IDs waiting to be registered
    static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> IncomingPostcards = new();

    static UserSession[] _sessions = [];
    static int _sessionCounter;

    static void Main(string[] args)
    {
        var scenario = Scenario.Create("postcard_flow", async context =>
            {
                // Assign a sticky session on first invocation for this virtual user
                if (!context.Data.ContainsKey("session"))
                {
                    var idx = Interlocked.Increment(ref _sessionCounter) - 1;
                    context.Data["session"] = _sessions[idx % _sessions.Length];
                }

                var session = (UserSession)context.Data["session"];

                // Drain the entire incoming queue before sending
                if (IncomingPostcards.TryGetValue(session.Username, out var queue))
                {
                    while (queue.TryDequeue(out var postcardNum))
                    {
                        var result = await RegisterPostcard(session, postcardNum, context.Logger);
                        if (result.IsError) return result;
                    }
                }

                return await SendPostcard(session, context.Logger);
            })
            .WithInit(async _ =>
            {
                _sessions = await Task.WhenAll(Users.Select(u => Login(u)));
                foreach (var s in _sessions)
                    IncomingPostcards[s.Username] = new ConcurrentQueue<string>();

                Console.WriteLine($"Logged in {_sessions.Length} users: {string.Join(", ", _sessions.Select(s => s.Username))}");
            })
            .WithClean(async _ =>
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var path = $"stats_{timestamp}.txt";

                var lines = new List<string> { $"Postcard stats — {timestamp}", "" };

                foreach (var session in _sessions)
                {
                    var response = await session.Client.GetAsync("/api/postcard/stats");
                    if (response.IsSuccessStatusCode)
                    {
                        var stats = await response.Content.ReadFromJsonAsync<PostcardStatsResponse>();
                        lines.Add($"{session.Username}: sent={stats!.PostcardsSent}, received={stats.PostcardsReceived}");
                    }
                    else
                    {
                        lines.Add($"{session.Username}: failed to fetch stats ({response.StatusCode})");
                    }
                }

                await File.WriteAllLinesAsync(path, lines);
                Console.WriteLine($"Stats written to {path}");
                foreach (var line in lines) Console.WriteLine(line);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: Users.Length, during: TimeSpan.FromSeconds(60))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestName("cpcx_postcard_flow")
            .Run();
    }

    static async Task<UserSession> Login(UserCredentials credentials)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            // Accept dev HTTPS certificate
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = credentials.Username, password = credentials.Password });

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Login failed for {credentials.Username}: {response.StatusCode}");

        Console.WriteLine($"Logged in: {credentials.Username}");
        return new UserSession(credentials.Username, client);
    }

    static async Task<IResponse> SendPostcard(UserSession session, Serilog.ILogger logger)
    {
        var response = await session.Client.PostAsync("/api/postcard/send", null);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.Warning("Send failed for {User}: {Status} {Body}", session.Username, response.StatusCode, body);
            return Response.Fail(message: $"send failed: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<SendPostcardResponse>();
        if (result == null)
            return Response.Fail(message: "send returned null response");

        // Parse the numeric part from "E26-42" -> "42"
        var numericId = result.PostcardId.Contains('-')
            ? result.PostcardId.Split('-', 2)[1]
            : result.PostcardId;

        IncomingPostcards.GetOrAdd(result.ReceiverUsername, _ => new ConcurrentQueue<string>())
                         .Enqueue(numericId);

        logger.Information("{Sender} sent postcard {Id} to {Receiver}", session.Username, result.PostcardId, result.ReceiverUsername);
        return Response.Ok();
    }

    static async Task<IResponse> RegisterPostcard(UserSession session, string postcardNum, Serilog.ILogger logger)
    {
        var response = await session.Client.PostAsJsonAsync("/api/postcard/register",
            new { PostcardId = postcardNum });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.Warning("Register failed for {User} postcard {Id}: {Status} {Body}", session.Username, postcardNum, response.StatusCode, body);
            return Response.Fail(message: $"register failed: {response.StatusCode}");
        }

        logger.Information("{Receiver} registered postcard {Id}", session.Username, postcardNum);
        return Response.Ok();
    }
}
