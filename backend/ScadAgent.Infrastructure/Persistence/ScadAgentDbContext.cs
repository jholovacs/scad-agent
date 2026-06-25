using Microsoft.EntityFrameworkCore;
using ScadAgent.Domain.Entities;

namespace ScadAgent.Infrastructure.Persistence;

public class ScadAgentDbContext : DbContext
{
    public ScadAgentDbContext(DbContextOptions<ScadAgentDbContext> options) : base(options)
    {
    }

    public DbSet<DesignSession> Sessions => Set<DesignSession>();
    public DbSet<DesignIteration> Iterations => Set<DesignIteration>();
    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DesignSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.HasMany(e => e.Iterations).WithOne(e => e.Session).HasForeignKey(e => e.SessionId);
            entity.HasMany(e => e.Messages).WithOne(e => e.Session).HasForeignKey(e => e.SessionId);
            entity.HasOne(e => e.CurrentIteration).WithMany().HasForeignKey(e => e.CurrentIterationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DesignIteration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScadContent).IsRequired();
            entity.Property(e => e.ScadHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.SessionId, e.Version }).IsUnique();
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
        });
    }
}
