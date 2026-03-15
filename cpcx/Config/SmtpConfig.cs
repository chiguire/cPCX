namespace cpcx.Config;

public class SmtpConfig
{
    public const string Smtp = "Smtp";
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool EnableSsl { get; init; } = true;
    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "DeerPost";
}
