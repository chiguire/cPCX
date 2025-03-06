using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities;

public class CpcxUser : IdentityUser<Guid>
{
    [ProtectedPersonalData]
    public Pronoun Pronouns { get; set; }
    
    [ProtectedPersonalData]
    [MaxLength(3000)] public string ProfileDescription { get; set; } = null!;
    
    public List<CpcxUser>? BlockedUsers { get; set; } = null!;
    
    public DateTime DeactivatedDate { get; set; } = DateTime.UnixEpoch;
    public DateTime BlockedUntilDate { get; set; } = DateTime.UnixEpoch;
}