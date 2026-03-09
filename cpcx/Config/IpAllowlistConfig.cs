namespace cpcx.Config;

public class IpAllowlistConfig
{
    public const string IpAllowlist = "IpAllowlist";
    public bool Enabled { get; init; } = false;
    public List<string> AllowedIPs { get; init; } = [];
}
