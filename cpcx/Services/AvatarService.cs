using cpcx.Config;
using cpcx.Entities;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public interface IAvatarService
{
    public List<string> GetAvatarListForUser(CpcxUser user);
    
    public string GetCurrentAvatarFullPathForUser(CpcxUser user);

    //public void UploadAvatarForUser(CpcxUser user, BinaryReader br); // TODO: when we allow users to upload their avatars
}

public class AvatarService(IOptions<CpcxConfig> cpcxConfig) : IAvatarService
{
    private readonly CpcxConfig _cpcxConfig = cpcxConfig.Value;
        
    private List<string>? _avatarList = null;

    public List<string> GetAvatarListForUser(CpcxUser _)
    {
        return _GetAvatarList();
    }

    public string GetCurrentAvatarFullPathForUser(CpcxUser user)
    {
        return _cpcxConfig.AvatarPath + "/" + user.AvatarPath;
    }

    private List<string> _GetAvatarList()
    {
        if (_avatarList != null) return _avatarList;
        var files = Directory.GetFiles(_cpcxConfig.AvatarPath);
        _avatarList = files.ToList().ConvertAll(Path.GetFileName)!;

        return _avatarList;
    }
}