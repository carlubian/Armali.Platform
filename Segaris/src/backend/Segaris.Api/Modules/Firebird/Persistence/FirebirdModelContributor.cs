using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Firebird.Persistence;

internal sealed class FirebirdModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCatalog<PersonCategory>(modelBuilder, "firebird_person_categories");
        ConfigureCatalog<UsernamePlatform>(modelBuilder, "firebird_username_platforms");

        modelBuilder.Entity<Person>(builder =>
        {
            builder.ToTable("firebird_people");
            builder.HasKey(person => person.Id);
            builder.Property(person => person.Id).ValueGeneratedOnAdd();
            builder.Property(person => person.Name).HasMaxLength(FirebirdValidation.NameMaximumLength).IsRequired();
            builder.Property(person => person.CategoryId).IsRequired();
            builder.Property(person => person.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(person => person.BirthdayMonth);
            builder.Property(person => person.BirthdayDay);
            builder.Property(person => person.Notes).HasMaxLength(FirebirdValidation.NotesMaximumLength);
            builder.Property(person => person.AvatarAttachmentId);
            builder.Property(person => person.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(person => person.CreatedAt).IsRequired();
            builder.Property(person => person.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_firebird_people_status", "\"Status\" IN ('Unknown', 'Active', 'Unavailable', 'Blocked')");
                table.HasCheckConstraint("CK_firebird_people_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint(
                    "CK_firebird_people_birthday",
                    "(\"BirthdayMonth\" IS NULL AND \"BirthdayDay\" IS NULL) OR (\"BirthdayMonth\" BETWEEN 1 AND 12 AND \"BirthdayDay\" BETWEEN 1 AND CASE WHEN \"BirthdayMonth\" = 2 THEN 29 WHEN \"BirthdayMonth\" IN (4, 6, 9, 11) THEN 30 ELSE 31 END)");
            });
            builder.HasOne<PersonCategory>().WithMany().HasForeignKey(person => person.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(person => person.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(person => person.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(person => new { person.Name, person.Id });
            builder.HasIndex(person => new { person.BirthdayMonth, person.BirthdayDay, person.Id });
            builder.HasIndex(person => new { person.CreatedBy, person.Visibility, person.Id });
            builder.HasIndex(person => new { person.Visibility, person.BirthdayMonth, person.BirthdayDay });
            builder.HasIndex(person => person.CategoryId);
            builder.HasIndex(person => person.Status);
            builder.HasIndex(person => person.Visibility);
            builder.HasIndex(person => person.UpdatedBy);
        });

        modelBuilder.Entity<Username>(builder =>
        {
            builder.ToTable("firebird_usernames");
            builder.HasKey(username => username.Id);
            builder.Property(username => username.Id).ValueGeneratedOnAdd();
            builder.Property(username => username.PersonId).IsRequired();
            builder.Property(username => username.PlatformId).IsRequired();
            builder.Property(username => username.Handle).HasMaxLength(FirebirdValidation.UsernameHandleMaximumLength).IsRequired();
            builder.Property(username => username.Notes).HasMaxLength(FirebirdValidation.UsernameNotesMaximumLength);
            builder.Property(username => username.CreatedAt).IsRequired();
            builder.Property(username => username.UpdatedAt).IsRequired();
            builder.HasOne<Person>().WithMany().HasForeignKey(username => username.PersonId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<UsernamePlatform>().WithMany().HasForeignKey(username => username.PlatformId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(username => username.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(username => username.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(username => new { username.PersonId, username.Id });
            builder.HasIndex(username => username.PlatformId);
            builder.HasIndex(username => username.UpdatedBy);
        });

        modelBuilder.Entity<Interaction>(builder =>
        {
            builder.ToTable("firebird_interactions");
            builder.HasKey(interaction => interaction.Id);
            builder.Property(interaction => interaction.Id).ValueGeneratedOnAdd();
            builder.Property(interaction => interaction.PersonId).IsRequired();
            builder.Property(interaction => interaction.Date).IsRequired();
            builder.Property(interaction => interaction.Description).HasMaxLength(FirebirdValidation.InteractionDescriptionMaximumLength).IsRequired();
            builder.Property(interaction => interaction.CreatedAt).IsRequired();
            builder.Property(interaction => interaction.UpdatedAt).IsRequired();
            builder.HasOne<Person>().WithMany().HasForeignKey(interaction => interaction.PersonId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(interaction => interaction.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(interaction => interaction.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(interaction => new { interaction.PersonId, interaction.Date, interaction.Id });
            builder.HasIndex(interaction => interaction.UpdatedBy);
        });
    }

    private static void ConfigureCatalog<TCatalog>(ModelBuilder modelBuilder, string tableName)
        where TCatalog : class
    {
        modelBuilder.Entity<TCatalog>(builder =>
        {
            builder.ToTable(tableName);
            builder.Property<int>("Id").ValueGeneratedOnAdd();
            builder.HasKey("Id");
            builder.Property<string>("Name").HasMaxLength(FirebirdValidation.CatalogNameMaximumLength).IsRequired();
            builder.Property<string>("NormalizedName").HasMaxLength(FirebirdValidation.CatalogNameMaximumLength).IsRequired();
            builder.Property<int>("SortOrder").IsRequired();
            builder.Property<DateTimeOffset>("CreatedAt").IsRequired();
            builder.Property<int?>("CreatedBy");
            builder.Property<DateTimeOffset>("UpdatedAt").IsRequired();
            builder.Property<int?>("UpdatedBy");
            builder.HasIndex("NormalizedName").IsUnique();
            builder.HasIndex("SortOrder");
        });
    }
}

