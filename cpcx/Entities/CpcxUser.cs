using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities;

public class CpcxUser : IdentityUser
{
    [MaxLength(32)] public string Alias { get; set; } = null!;
    [MaxLength(32)] public string Pronouns { get; set; } = null!;
    public List<CpcxUser> BlockedUsers { get; set; } = null!;
    public DateTime DeactivatedDate { get; set; } = DateTime.UnixEpoch;
    public DateTime BlockedUntilDate { get; set; } = DateTime.UnixEpoch;
}