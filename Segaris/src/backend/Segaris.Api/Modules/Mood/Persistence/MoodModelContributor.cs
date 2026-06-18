using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Mood.Persistence;

internal sealed class MoodModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MoodEntry>(builder =>
        {
            builder.ToTable("mood_entries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Id).ValueGeneratedOnAdd();
            builder.Property(entry => entry.EntryDate).IsRequired();
            builder.Property(entry => entry.Score).IsRequired();
            builder.Property(entry => entry.Energy).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.Alignment).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.Source).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.Notes).HasMaxLength(MoodDefaults.NotesMaxLength);
            builder.Property(entry => entry.CreatedAt).IsRequired();
            builder.Property(entry => entry.CreatedBy).IsRequired();
            builder.Property(entry => entry.UpdatedAt);
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_mood_entries_score",
                    $"\"Score\" >= {MoodDefaults.ScoreMinimum} AND \"Score\" <= {MoodDefaults.ScoreMaximum}");
                table.HasCheckConstraint("CK_mood_entries_energy", EnumConstraint<MoodEnergy>("Energy"));
                table.HasCheckConstraint("CK_mood_entries_alignment", EnumConstraint<MoodAlignment>("Alignment"));
                table.HasCheckConstraint("CK_mood_entries_direction", EnumConstraint<MoodDirection>("Direction"));
                table.HasCheckConstraint("CK_mood_entries_source", EnumConstraint<MoodSource>("Source"));
            });
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(entry => entry.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(entry => entry.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(entry => new { entry.CreatedBy, entry.EntryDate, entry.Id });
            builder.HasIndex(entry => new { entry.CreatedBy, entry.Id });
            builder.HasIndex(entry => new { entry.CreatedBy, entry.EntryDate });
            builder.HasIndex(entry => entry.UpdatedBy);
        });
    }

    private static string EnumConstraint<TEnum>(string column)
        where TEnum : struct, Enum
    {
        var values = Enum.GetNames<TEnum>()
            .Select(value => $"'{value}'");
        return $"\"{column}\" IN ({string.Join(", ", values)})";
    }
}
