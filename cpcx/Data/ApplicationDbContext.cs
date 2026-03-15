using cpcx.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace cpcx.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
    : IdentityDbContext<CpcxUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CpcxUser>()
            .HasMany(u => u.BlockedUsers)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserBlocks",
                j => j.HasOne<CpcxUser>().WithMany().HasForeignKey("BlockedUserId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<CpcxUser>().WithMany().HasForeignKey("BlockerId").OnDelete(DeleteBehavior.Cascade)
            );

        builder.Entity<Event>(e =>
        {
            e.HasIndex(e => e.PublicId).IsUnique();
        });

        builder.Entity<EventUser>(eu =>
        {
            eu.HasKey(eu_ => new {eu_.EventId, eu_.UserId});
        });

        builder.Entity<Postcard>(p =>
        {
            p.HasKey(p => p.Id);
            p.Property(p => p.SentOn)
                .HasDefaultValueSql("NOW()");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseSeeding((context, b) => Seed(context, b).GetAwaiter().GetResult())
            .UseAsyncSeeding(Seed);

    public DbSet<Event> Events { get; set; }
    public DbSet<EventUser> EventUsers { get; set; }
    public DbSet<Postcard> Postcards { get; set; }

    // Produces stable GUIDs for seeded entities: 00000000-0000-0000-0000-000000000001, etc.
    private static Guid SeededGuid(int i) => new Guid($"00000000-0000-0000-0000-{i:D12}");

    private async Task Seed(DbContext context, bool b, CancellationToken token = default)
    {
        var hasher = new PasswordHasher<CpcxUser>();
        var devpassword = hasher.HashPassword(null!, "devpassword");
        var eventStart = new DateTime(year: 2026, month: 7, day: 15, hour: 0, minute: 45, second: 0, kind: DateTimeKind.Utc);

        var initialEvent = new Event
        {
            Id = new Guid("07306f05-a716-4ba4-b19a-52c3bc9241fb"),
            Name = "Electromagnetic Field 2026",
            ShortName = "EMF 2026",
            URL = "https://www.emfcamp.org",
            PublicId = "E26",
            Venue = "Eastnor Castle Deer Park",
            Start = eventStart,
            End = eventStart.AddDays(5),
            Open = true,
            Visible = true,
        };

        var zones = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
        var userList = Enumerable.Range(1, 200).Select(i => new CpcxUser
        {
            Id = SeededGuid(i),
            UserName = $"user{i:D3}",
            NormalizedUserName = $"USER{i:D3}",
            Email = $"user{i:D3}@example.com",
            NormalizedEmail = $"USER{i:D3}@EXAMPLE.COM",
            PasswordHash = devpassword,
            Pronouns = Pronoun.Neutral,
            ProfileDescription = $"Load test user {i}",
            BlockedUsers = [],
            SecurityStamp = SeededGuid(i + 1000).ToString(),
        }).ToList();

        var eventUserList = userList.Select((u, idx) => new EventUser
        {
            EventId = initialEvent.Id,
            UserId = u.Id,
            Address = $"Zone {zones[idx % zones.Length]}, Tent {idx / zones.Length + 1}",
            ActiveInEvent = true,
        }).ToList();

        var ev = context.Set<Event>().FirstOrDefault(e => e.Id == initialEvent.Id);
        if (ev == null)
            context.Set<Event>().Add(initialEvent);

        if (configuration.GetValue<bool>("Cpcx:EnableSeed"))
        {
            var existingUser = context.Set<CpcxUser>().FirstOrDefault(u => u.Id == userList[0].Id);
            if (existingUser == null)
                context.Set<CpcxUser>().AddRange(userList);

            var existingEventUser = context.Set<EventUser>().FirstOrDefault(u => u.EventId == initialEvent.Id);
            if (existingEventUser == null)
                context.Set<EventUser>().AddRange(eventUserList);
        }

        await context.SaveChangesAsync(token);
    }
}
