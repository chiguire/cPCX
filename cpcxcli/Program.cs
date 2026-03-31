using System.CommandLine;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using cpcx.Data;
using cpcx.Entities;

var appConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("CPCX_")
    .Build();

// ── Helpers ──────────────────────────────────────────────────────────────────

ApplicationDbContext CreateDb(string connection)
{
    var resolved = !string.IsNullOrWhiteSpace(connection) ? connection
        : Environment.GetEnvironmentVariable("CPCX_CONNECTION_STRING")
          ?? appConfig.GetConnectionString("DefaultConnection")
          ?? "";
    if (string.IsNullOrWhiteSpace(resolved))
    {
        Console.Error.WriteLine("No connection string provided. Use --connection or set CPCX_CONNECTION_STRING.");
        Environment.Exit(1);
    }
    var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(resolved)
        .Options;
    return new ApplicationDbContext(opts, new ConfigurationBuilder().Build());
}

async Task<Event> ResolveEvent(ApplicationDbContext db, string? publicId)
{
    var id = publicId ?? appConfig["Cpcx:ActiveEventId"] ?? "E26";
    var ev = await db.Events.FirstOrDefaultAsync(e => e.PublicId == id);
    if (ev is null)
    {
        Console.Error.WriteLine($"Event '{id}' not found.");
        Environment.Exit(1);
    }
    return ev!;
}

async Task<CpcxUser> ResolveUser(ApplicationDbContext db, string username)
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == username);
    if (user is null)
    {
        Console.Error.WriteLine($"User '{username}' not found.");
        Environment.Exit(1);
    }
    return user!;
}

TimeSpan ParseDuration(string s)
{
    var m = Regex.Match(s.Trim(), @"^(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?$");
    int d   = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 0;
    int h   = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
    int min = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
    int sec = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
    if (d == 0 && h == 0 && min == 0 && sec == 0)
    {
        throw new FormatException($"Invalid duration '{s}'. Expected format: 1d, 2h30m, 45m, 1d12h30m15s.");
    }
    return new TimeSpan(d, h, min, sec);
}

// ── Root & global options ────────────────────────────────────────────────────

var root = new RootCommand("cpcxcli — DeerPost.cx database administration");

var connectionOpt = new Option<string>(
    "--connection",
    "PostgreSQL connection string. Falls back to CPCX_CONNECTION_STRING env var, then appsettings.json.");
root.AddGlobalOption(connectionOpt);

// ── event commands ────────────────────────────────────────────────────────────

var eventCmd = new Command("event", "Manage events");
root.AddCommand(eventCmd);

// event open
{
    var evOpt = new Option<string?>("--event", "Event public ID (e.g. E26). Defaults to Cpcx:ActiveEventId.");
    var cmd = new Command("open", "Open an event for postcard sending");
    cmd.AddOption(evOpt);
    cmd.SetHandler(async (conn, ev) =>
    {
        await using var db = CreateDb(conn);
        var e = await ResolveEvent(db, ev);
        e.Open = true;
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] {e.PublicId} ({e.Name}) is now open.");
    }, connectionOpt, evOpt);
    eventCmd.AddCommand(cmd);
}

// event close
{
    var evOpt = new Option<string?>("--event", "Event public ID (e.g. E26). Defaults to Cpcx:ActiveEventId.");
    var cmd = new Command("close", "Close an event");
    cmd.AddOption(evOpt);
    cmd.SetHandler(async (conn, ev) =>
    {
        await using var db = CreateDb(conn);
        var e = await ResolveEvent(db, ev);
        e.Open = false;
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] {e.PublicId} ({e.Name}) is now closed.");
    }, connectionOpt, evOpt);
    eventCmd.AddCommand(cmd);
}

// event set-dates
{
    var evOpt    = new Option<string?>("--event", "Event public ID. Defaults to Cpcx:ActiveEventId.");
    var startOpt = new Option<DateTime>("--start", "Start date/time in UTC (ISO 8601)") { IsRequired = true };
    var endOpt   = new Option<DateTime>("--end",   "End date/time in UTC (ISO 8601)")   { IsRequired = true };
    var cmd = new Command("set-dates", "Set start and end dates for an event");
    cmd.AddOption(evOpt);
    cmd.AddOption(startOpt);
    cmd.AddOption(endOpt);
    cmd.SetHandler(async (conn, ev, start, end) =>
    {
        await using var db = CreateDb(conn);
        var e = await ResolveEvent(db, ev);
        e.Start = new DateTimeOffset(DateTime.SpecifyKind(start, DateTimeKind.Utc));
        e.End   = new DateTimeOffset(DateTime.SpecifyKind(end,   DateTimeKind.Utc));
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] {e.PublicId} ({e.Name}): {e.Start:u} — {e.End:u}");
    }, connectionOpt, evOpt, startOpt, endOpt);
    eventCmd.AddCommand(cmd);
}

// ── user commands ─────────────────────────────────────────────────────────────

var userCmd = new Command("user", "Manage users");
root.AddCommand(userCmd);

// user block
{
    var usernameArg = new Argument<string>("username");
    var forOpt = new Option<string?>("--for", "Block duration (e.g. 1d, 2h30m, 1d12h). Omit for indefinite block.");
    var cmd = new Command("block", "Admin-block a user");
    cmd.AddArgument(usernameArg);
    cmd.AddOption(forOpt);
    cmd.SetHandler(async (conn, username, forDur) =>
    {
        await using var db = CreateDb(conn);
        var user = await ResolveUser(db, username);
        user.BlockedUntilDate = forDur is not null
            ? DateTimeOffset.UtcNow.Add(ParseDuration(forDur))
            : DateTimeOffset.MaxValue;
        await db.SaveChangesAsync();
        var until = user.BlockedUntilDate == DateTimeOffset.MaxValue
            ? "indefinitely"
            : $"until {user.BlockedUntilDate:u} UTC";
        Console.WriteLine($"[OK] '{username}' blocked {until}.");
    }, connectionOpt, usernameArg, forOpt);
    userCmd.AddCommand(cmd);
}

// user unblock
{
    var usernameArg = new Argument<string>("username");
    var cmd = new Command("unblock", "Remove an admin block from a user");
    cmd.AddArgument(usernameArg);
    cmd.SetHandler(async (conn, username) =>
    {
        await using var db = CreateDb(conn);
        var user = await ResolveUser(db, username);
        user.BlockedUntilDate = DateTimeOffset.UnixEpoch;
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] '{username}' unblocked.");
    }, connectionOpt, usernameArg);
    userCmd.AddCommand(cmd);
}

// user deactivate
{
    var usernameArg = new Argument<string>("username");
    var cmd = new Command("deactivate", "Deactivate a user account");
    cmd.AddArgument(usernameArg);
    cmd.SetHandler(async (conn, username) =>
    {
        await using var db = CreateDb(conn);
        var user = await ResolveUser(db, username);
        user.DeactivatedDate = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] '{username}' deactivated.");
    }, connectionOpt, usernameArg);
    userCmd.AddCommand(cmd);
}

// user activate
{
    var usernameArg = new Argument<string>("username");
    var cmd = new Command("activate", "Reactivate a deactivated user");
    cmd.AddArgument(usernameArg);
    cmd.SetHandler(async (conn, username) =>
    {
        await using var db = CreateDb(conn);
        var user = await ResolveUser(db, username);
        user.DeactivatedDate = DateTimeOffset.UnixEpoch;
        await db.SaveChangesAsync();
        Console.WriteLine($"[OK] '{username}' reactivated.");
    }, connectionOpt, usernameArg);
    userCmd.AddCommand(cmd);
}

return await root.InvokeAsync(args);