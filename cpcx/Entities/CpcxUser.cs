using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities;

public class CpcxUser : IdentityUser<Guid>
{
    [PersonalData]
    public Pronoun Pronouns { get; set; }

    [PersonalData] [MaxLength(3000)] public string ProfileDescription { get; set; } = "";
    
    [PersonalData] [MaxLength(100)] public string AvatarPath { get; set; } = "";
    
    public List<CpcxUser>? BlockedUsers { get; set; } = null!;
    
    public DateTime DeactivatedDate { get; set; } = DateTime.UnixEpoch;
    public DateTime BlockedUntilDate { get; set; } = DateTime.UnixEpoch;
}