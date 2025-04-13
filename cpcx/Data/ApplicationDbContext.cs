using cpcx.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<CpcxUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        var hasher = new PasswordHasher<CpcxUser>();
        var devpassword = hasher.HashPassword(null!, "devpassword");
        

        var initialEvent = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Electromagnetic Field 2026",
            ShortName = "EMF 2026",
            URL = "https://www.emfcamp.org",
            PublicId = "E26",
            Venue = "Eastnor Castle Deer Park",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddDays(7),
            Open = true,
            Visible = true,
        };
        var initialUserList = new List<CpcxUser>
        {
            new CpcxUser
            {
                Id = Guid.NewGuid(),
                UserName = "aaa",
                NormalizedUserName = "AAA",
                Email = "a@example.com",
                NormalizedEmail = "A@EXAMPLE.COM",
                PasswordHash = devpassword,
                Pronouns = Pronoun.Male,
                ProfileDescription = "",
                BlockedUsers = [],
                SecurityStamp = Guid.NewGuid().ToString(),
            },
            new CpcxUser
            {
                Id = Guid.NewGuid(),
                UserName = "bbb",
                NormalizedUserName = "BBB",
                Email = "b@example.com",
                NormalizedEmail = "B@EXAMPLE.COM",
                PasswordHash = devpassword,
                Pronouns = Pronoun.Female,
                ProfileDescription = "",
                BlockedUsers = [],
                SecurityStamp = Guid.NewGuid().ToString(),
            },
            new CpcxUser
            {
                Id = Guid.NewGuid(),
                UserName = "ccc",
                NormalizedUserName = "CCC",
                Email = "c@example.com",
                NormalizedEmail = "C@EXAMPLE.COM",
                PasswordHash = devpassword,
                Pronouns = Pronoun.Neutral,
                ProfileDescription = "",
                BlockedUsers = [],
                SecurityStamp = Guid.NewGuid().ToString(),
            },
        };
        var initialEventUserList = new List<EventUser>
        {
            new EventUser
            {
                EventId = initialEvent.Id,
                UserId = initialUserList[0].Id,
                Address = "Tent 1, Zone A",
                ActiveInEvent = true,
            },
            new EventUser
            {
                EventId = initialEvent.Id,
                UserId = initialUserList[1].Id,
                Address = "Tent 1, Zone B",
                ActiveInEvent = true,
            },
            new EventUser
            {
                EventId = initialEvent.Id,
                UserId = initialUserList[2].Id,
                Address = "Tent 2, Zone B",
                ActiveInEvent = true,
            }
        };

        builder.Entity<CpcxUser>(u =>
        {
            u.HasData(initialUserList);
        });
        
        builder.Entity<Event>(e =>
        {
            e.HasIndex(e => e.PublicId).IsUnique();
            e.HasData(initialEvent);
        });

        builder.Entity<EventUser>(eu =>
        {
            eu.HasKey(eu_ => new {eu_.EventId, eu_.UserId});
            eu.HasData(initialEventUserList);
        });

        builder.Entity<Postcard>(p =>
        {
            p.HasKey(p => p.Id);
            p.Property(p => p.SentOn)
                .HasDefaultValueSql("datetime()");
        });
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<EventUser> EventUsers { get; set; }
    public DbSet<Postcard> Postcards { get; set; }
}
