using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Calendar.Persistence;

internal sealed class CalendarModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalendarDailyNote>(builder =>
        {
            builder.ToTable("calendar_daily_notes");
            builder.HasKey(note => note.Id);
            builder.Property(note => note.Id).ValueGeneratedOnAdd();
            builder.Property(note => note.Date).IsRequired();
            builder.Property(note => note.Title).HasMaxLength(CalendarDefaults.TitleMaximumLength);
            builder.Property(note => note.Body).HasMaxLength(CalendarDefaults.BodyMaximumLength).IsRequired();
            builder.Property(note => note.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(note => note.CreatedAt).IsRequired();
            builder.Property(note => note.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_calendar_daily_notes_visibility", "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(note => note.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(note => note.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(note => new { note.Date, note.Id });
            builder.HasIndex(note => new { note.Visibility, note.Date, note.Id });
            builder.HasIndex(note => new { note.CreatedBy, note.Visibility, note.Date, note.Id });
            builder.HasIndex(note => new { note.CreatedBy, note.Id });
            builder.HasIndex(note => note.UpdatedBy);
        });
    }
}
