using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Games.Persistence;

internal sealed class GamesModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(builder =>
        {
            builder.ToTable("games_games");
            builder.HasKey(game => game.Id);
            builder.Property(game => game.Id).ValueGeneratedOnAdd();
            builder.Property(game => game.Name).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(game => game.NormalizedName).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(game => game.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(game => game.SortOrder).IsRequired();
            builder.Property(game => game.CreatedAt).IsRequired();
            builder.Property(game => game.CreatedBy);
            builder.Property(game => game.UpdatedAt).IsRequired();
            builder.Property(game => game.UpdatedBy);
            builder.ToTable(table =>
                table.HasCheckConstraint("CK_games_games_platform", "\"Platform\" IN ('PC', 'Console', 'Mobile', 'BoardGame', 'TabletopRpg', 'Other')"));
            builder.HasIndex(game => game.NormalizedName).IsUnique();
            builder.HasIndex(game => new { game.SortOrder, game.Id });
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(game => game.CreatedBy).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(game => game.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Playthrough>(builder =>
        {
            builder.ToTable("games_playthroughs");
            builder.HasKey(playthrough => playthrough.Id);
            builder.Property(playthrough => playthrough.Id).ValueGeneratedOnAdd();
            builder.Property(playthrough => playthrough.GameId).IsRequired();
            builder.Property(playthrough => playthrough.Name).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(playthrough => playthrough.NormalizedName).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(playthrough => playthrough.StartYear).IsRequired();
            builder.Property(playthrough => playthrough.StartMonth).IsRequired();
            builder.Property(playthrough => playthrough.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(playthrough => playthrough.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(playthrough => playthrough.CreatedAt).IsRequired();
            builder.Property(playthrough => playthrough.CreatedBy).IsRequired();
            builder.Property(playthrough => playthrough.UpdatedAt).IsRequired();
            builder.Property(playthrough => playthrough.UpdatedBy).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_games_playthroughs_start_month", "\"StartMonth\" BETWEEN 1 AND 12");
                table.HasCheckConstraint("CK_games_playthroughs_start_year", "\"StartYear\" BETWEEN 1 AND 9999");
                table.HasCheckConstraint("CK_games_playthroughs_status", "\"Status\" IN ('Planning', 'Active', 'Completed')");
                table.HasCheckConstraint("CK_games_playthroughs_visibility", "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<Game>().WithMany().HasForeignKey(playthrough => playthrough.GameId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(playthrough => playthrough.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(playthrough => playthrough.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(playthrough => playthrough.GameId);
            builder.HasIndex(playthrough => new { playthrough.CreatedBy, playthrough.Visibility, playthrough.Id });
            builder.HasIndex(playthrough => new { playthrough.NormalizedName, playthrough.Id });
            builder.HasIndex(playthrough => new { playthrough.StartYear, playthrough.StartMonth, playthrough.Id });
            builder.HasIndex(playthrough => new { playthrough.Status, playthrough.Id });
            builder.HasIndex(playthrough => playthrough.Visibility);
        });

        modelBuilder.Entity<PlaythroughTag>(builder =>
        {
            builder.ToTable("games_playthrough_tags");
            builder.HasKey(tag => tag.Id);
            builder.Property(tag => tag.Id).ValueGeneratedOnAdd();
            builder.Property(tag => tag.PlaythroughId).IsRequired();
            builder.Property(tag => tag.Value).HasMaxLength(GamesDefaults.TagMaximumLength).IsRequired();
            builder.Property(tag => tag.NormalizedValue).HasMaxLength(GamesDefaults.TagMaximumLength).IsRequired();
            builder.Property(tag => tag.SortOrder).IsRequired();
            builder.HasOne<Playthrough>().WithMany().HasForeignKey(tag => tag.PlaythroughId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(tag => new { tag.PlaythroughId, tag.NormalizedValue }).IsUnique();
            builder.HasIndex(tag => new { tag.NormalizedValue, tag.PlaythroughId });
            builder.HasIndex(tag => new { tag.PlaythroughId, tag.SortOrder, tag.Id });
        });

        modelBuilder.Entity<Section>(builder =>
        {
            builder.ToTable("games_sections");
            builder.HasKey(section => section.Id);
            builder.Property(section => section.Id).ValueGeneratedOnAdd();
            builder.Property(section => section.PlaythroughId).IsRequired();
            builder.Property(section => section.Name).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(section => section.NormalizedName).HasMaxLength(GamesDefaults.NameMaximumLength).IsRequired();
            builder.Property(section => section.Color).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(section => section.SortOrder).IsRequired();
            builder.Property(section => section.CreatedAt).IsRequired();
            builder.Property(section => section.CreatedBy).IsRequired();
            builder.Property(section => section.UpdatedAt).IsRequired();
            builder.Property(section => section.UpdatedBy).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint("CK_games_sections_color", "\"Color\" IN ('Blue', 'Green', 'Amber', 'Red', 'Purple', 'Pink', 'Teal', 'Indigo', 'Slate', 'Orange')"));
            builder.HasOne<Playthrough>().WithMany().HasForeignKey(section => section.PlaythroughId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(section => section.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(section => section.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(section => new { section.PlaythroughId, section.NormalizedName }).IsUnique();
            builder.HasIndex(section => new { section.PlaythroughId, section.SortOrder, section.Id });
        });

        modelBuilder.Entity<Goal>(builder =>
        {
            builder.ToTable("games_goals");
            builder.HasKey(goal => goal.Id);
            builder.Property(goal => goal.Id).ValueGeneratedOnAdd();
            builder.Property(goal => goal.SectionId).IsRequired();
            builder.Property(goal => goal.Text).HasMaxLength(GamesDefaults.GoalTextMaximumLength).IsRequired();
            builder.Property(goal => goal.Completed).IsRequired();
            builder.Property(goal => goal.Position).IsRequired();
            builder.Property(goal => goal.CreatedAt).IsRequired();
            builder.Property(goal => goal.CreatedBy).IsRequired();
            builder.Property(goal => goal.UpdatedAt).IsRequired();
            builder.Property(goal => goal.UpdatedBy).IsRequired();
            builder.HasOne<Section>().WithMany().HasForeignKey(goal => goal.SectionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(goal => goal.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(goal => goal.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(goal => new { goal.SectionId, goal.Position, goal.Id });
            builder.HasIndex(goal => goal.Completed);
        });
    }
}
