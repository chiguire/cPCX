using cpcx.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace cpcx.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(u =>
        {
            u.Property(e => e.Alias)
                .IsRequired()
                .HasMaxLength(32);
            u.Property(e => e.Pronouns)
                .IsRequired()
                .HasMaxLength(32);
            u.HasMany(u => u.Events)
                .WithMany(e => e.Users);
            u.HasMany(u => u.PostcardsSent)
                .WithOne(p => p.Sender);
            u.HasMany(u => u.PostcardsReceived)
                .WithOne(p => p.Receiver);

            u.HasData([
                new User
                {
                    Id = "1",
                    Email = "a@example.com",
                    Alias = "aaa",
                    PhoneNumber = "6543",
                    EmailConfirmed = true,
                },
                new User
                {
                    Id = "2",
                    Email = "b@example.com",
                    Alias = "bbb",
                    PhoneNumber = "6544",
                    EmailConfirmed = true,
                },
                new User
                {
                    Id = "3",
                    Email = "a@example.com",
                    Alias = "ccc",
                    PhoneNumber = "6545",
                    EmailConfirmed = true,
                },
            ]);
        });

        builder.Entity<Event>(e =>
        {
            e.HasKey(e => e.Id);
            e.HasMany(e => e.Postcards)
                .WithOne(e => e.Event).IsRequired();
            e.HasMany(e => e.Users)
                .WithMany(u => u.Events);
            e.HasData(new Event
            {
                Id = new Guid().ToString(),
                Name = "EMF 2026",
                Venue = "Eastnor Castle Deer Park",
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddDays(7),
                Postcards = [],
                Users = [],
            });
        });

        builder.Entity<Postcard>(p =>
        {
            p.HasKey(p => p.Id);
            p.Property(p => p.SentOn)
                .HasDefaultValueSql("datetime()");
            p.HasOne(p => p.Sender)
                .WithMany(u => u.PostcardsSent)
                .HasForeignKey(e => e.SenderId);
            p.HasOne(p => p.Receiver)
                .WithMany(u => u.PostcardsReceived)
                .HasForeignKey(e => e.ReceiverId);
            p.HasOne(p => p.Event)
                .WithMany(p => p.Postcards)
                .HasForeignKey(e => e.EventId);
        });
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Postcard> Postcards { get; set; }
}
