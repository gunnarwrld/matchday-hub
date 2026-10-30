using MatchdayHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchdayHub.Infrastructure.Persistence.Configurations;

/// <summary>Fluent mapping for <see cref="Competition"/>.</summary>
public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("competitions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Season).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.ExternalId).IsUnique();
    }
}

/// <summary>Fluent mapping for <see cref="Team"/>.</summary>
public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.ShortName).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Stadium).HasMaxLength(200);
        builder.HasIndex(t => t.ExternalId).IsUnique();
        builder.HasIndex(t => t.Name);
    }
}

/// <summary>Fluent mapping for <see cref="Match"/>.</summary>
public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(m => m.ExternalId).IsUnique();
        builder.HasIndex(m => m.KickoffUtc);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.HomeTeamId);
        builder.HasIndex(m => m.AwayTeamId);

        builder.HasOne(m => m.Competition)
            .WithMany(c => c.Matches)
            .HasForeignKey(m => m.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Fluent mapping for <see cref="MatchEvent"/>.</summary>
public sealed class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
{
    public void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        builder.ToTable("match_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UpstreamEventId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PlayerName).HasMaxLength(200);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

        // The idempotency anchor: upstream event ids are globally unique, so a
        // duplicate insert violates this index and is swallowed by the consumer.
        builder.HasIndex(e => e.UpstreamEventId).IsUnique();
        builder.HasIndex(e => e.MatchId);

        builder.HasOne(e => e.Match)
            .WithMany(m => m.Events)
            .HasForeignKey(e => e.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Team)
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Fluent mapping for <see cref="PlayerMatchLoad"/>.</summary>
public sealed class PlayerMatchLoadConfiguration : IEntityTypeConfiguration<PlayerMatchLoad>
{
    public void Configure(EntityTypeBuilder<PlayerMatchLoad> builder)
    {
        builder.ToTable("player_match_loads");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PlayerName).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => new { p.MatchId, p.PlayerName }).IsUnique();
        builder.HasIndex(p => p.MatchId);

        builder.HasOne(p => p.Match)
            .WithMany(m => m.PlayerLoads)
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Team)
            .WithMany()
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Fluent mapping for the idempotency ledger.</summary>
public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(p => p.MessageId);
        builder.Property(p => p.MessageId).HasMaxLength(100);
        builder.Property(p => p.MessageType).HasMaxLength(100).IsRequired();
    }
}
