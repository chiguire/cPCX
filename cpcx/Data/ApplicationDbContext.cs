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
                ProfileDescription = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin scelerisque feugiat mollis. Sed ut neque a nisl aliquam pharetra quis eget neque. Pellentesque eget consequat sem. Mauris volutpat, enim eu facilisis condimentum, felis metus pellentesque purus, auctor efficitur justo turpis ac erat. Nullam elit sapien, convallis eget finibus eget, tincidunt at neque. Mauris lectus quam, porta non turpis at, pulvinar venenatis velit. In odio dui, lacinia sit amet porttitor in, semper ullamcorper lectus. Pellentesque rutrum vehicula odio ut tristique. Mauris in tellus ut libero imperdiet rhoncus. Donec dolor nisl, tempus vitae fermentum quis, tincidunt eu tortor. Aliquam erat volutpat. Aliquam sem magna, mattis vitae mattis a, efficitur consectetur sapien. Donec dignissim, magna et rutrum dictum, mi justo ultricies magna, non bibendum massa orci feugiat dui. Cras pharetra, quam nec scelerisque accumsan, mauris tellus ultrices ligula, eget fermentum sem leo sed sapien. ",
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
                ProfileDescription = "Quisque at condimentum erat. Aenean pretium tortor at ex rutrum sollicitudin. Sed massa nisi, sollicitudin vitae bibendum iaculis, eleifend eu leo. Proin eu erat ut purus varius molestie at ac urna. Donec ut ultrices enim, vel euismod quam. Nulla mollis placerat metus, nec faucibus purus ultricies in. Praesent imperdiet finibus ultrices. Maecenas elementum augue eu urna faucibus sodales. Maecenas quis turpis sed risus rutrum gravida. Donec mollis mi sit amet elit luctus hendrerit. Quisque et quam at augue lobortis vehicula. Nullam accumsan neque at lorem gravida feugiat. Sed non odio eget risus suscipit vehicula in quis metus. Sed quis risus arcu. Phasellus nec lacinia erat. ",
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
                ProfileDescription = "Suspendisse sit amet est eu mauris maximus congue et at dolor. Phasellus non felis in odio placerat fermentum. Nulla facilisi. Morbi elementum et massa at auctor. Cras pulvinar bibendum ipsum, sagittis sagittis lorem. Aenean sollicitudin sapien at facilisis vulputate. Pellentesque scelerisque ex erat, in vestibulum massa mollis eu. Suspendisse tristique nisl eu fermentum feugiat. Mauris imperdiet euismod dui. Integer neque sem, malesuada et nisi et, commodo egestas dolor. Pellentesque id dolor fringilla, volutpat eros quis, lobortis magna. Donec fringilla leo nibh, id cursus tellus tempus ut. Duis ut velit faucibus, tempus diam sed, eleifend ante. Donec lacus libero, tempus vitae nunc non, maximus malesuada dui. Morbi accumsan sed libero a placerat. ",
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
