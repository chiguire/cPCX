using cpcx.Config;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public interface IAvatarService
{
    string GetAvatarFullPath(string avatarPath);
}

public class AvatarService(IOptions<CpcxConfig> cpcxConfig) : IAvatarService
{
    private readonly CpcxConfig _cpcxConfig = cpcxConfig.Value;
        
    private List<string>? _avatarList = null;

    public List<string> GetAvatarList()
    {
        return _GetAvatarList();
    }

    public string GetAvatarFullPath(string avatarPath)
    {
        return _cpcxConfig.AvatarPath + "/" + avatarPath;
    }

    private List<string> _GetAvatarList()
    {
        if (_avatarList != null) return _avatarList;
        var files = Directory.GetFiles(_cpcxConfig.AvatarPath);
        _avatarList = files.ToList().ConvertAll(Path.GetFileName)!;

        return _avatarList;
    }
}