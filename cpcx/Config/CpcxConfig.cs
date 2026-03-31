namespace cpcx.Config;

public class CpcxConfig
{
    public const string Cpcx = "Cpcx";
    public string ActiveEventId { get; init; } = "E26";
    public string AvatarPath { get; init; } = "";
    public bool EnableLoadTestApi { get; init; } = false;
    public bool EnableRegistration { get; init; } = true;
    public int PageSize { get; init; } = 10;
    public string CaretakerEmail { get; init; } = "";
}