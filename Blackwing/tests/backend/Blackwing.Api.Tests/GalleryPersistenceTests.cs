using Blackwing.Api.Gallery;
using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Blackwing.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Api.Tests;

[Collection(PostgresCollection.Name)]
public sealed class GalleryPersistenceTests(PostgresFixture fixture)
{
    private const string ShaA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ShaB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Migrations_create_the_gallery_schema_and_persist_an_image()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var image = Image.Create(new ImageValues(ShaA, "image/jpeg", 4000, 3000, 1_000_000, Now), owner, Now);
        database.Images.Add(image);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var stored = await database.Images.SingleAsync(value => value.Id == image.Id);
        Assert.Equal(ShaA, stored.Sha256);
        Assert.Null(stored.ReviewedAt);
        Assert.Equal(Now, stored.UploadedAt);
    }

    [Fact]
    public async Task Deduplication_is_unique_per_user_but_independent_across_users()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var alice = await PostgresFixture.CreateUserAsync(database, $"alice-{Guid.NewGuid():N}");
        var bob = await PostgresFixture.CreateUserAsync(database, $"bob-{Guid.NewGuid():N}");

        database.Images.Add(Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), alice, Now));
        await database.SaveChangesAsync();

        // The same bytes for a different owner are a separate, allowed row.
        database.Images.Add(Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), bob, Now));
        await database.SaveChangesAsync();

        // The same bytes for the same owner violate UNIQUE(OwnerUserId, Sha256).
        database.Images.Add(Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), alice, Now));
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task Get_or_create_tag_reuses_the_normalized_label_within_owner_and_type()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"tagger-{Guid.NewGuid():N}");

        var first = await service.GetOrCreateTagAsync(owner, TagType.Place, "Beach");
        var second = await service.GetOrCreateTagAsync(owner, TagType.Place, "  beach ");
        Assert.Equal(first.Id, second.Id);

        // A different type with the same label is a distinct tag.
        var topic = await service.GetOrCreateTagAsync(owner, TagType.Topic, "Beach");
        Assert.NotEqual(first.Id, topic.Id);

        // The unique index rejects a hand-rolled duplicate.
        database.Tags.Add(Tag.Create(owner, TagType.Place, "BEACH"));
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task Owner_isolation_prevents_touching_another_users_content()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var alice = await PostgresFixture.CreateUserAsync(database, $"alice-{Guid.NewGuid():N}");
        var bob = await PostgresFixture.CreateUserAsync(database, $"bob-{Guid.NewGuid():N}");

        var image = Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), alice, Now);
        database.Images.Add(image);
        var aliceTag = await service.GetOrCreateTagAsync(alice, TagType.Person, "Alice");
        await database.SaveChangesAsync();

        // Bob cannot see Alice's rows under his own scope.
        Assert.Empty(await database.Images.Where(value => value.OwnerUserId == bob).ToListAsync());
        Assert.Empty(await database.Tags.Where(value => value.OwnerUserId == bob).ToListAsync());

        // Bob cannot mutate Alice's image or merge her tags.
        Assert.False(await service.SetImageTagsAsync(image.Id, bob, [aliceTag.Id]));
        Assert.False(await service.DeleteImageAsync(image.Id, bob));
        var bobTag = await service.GetOrCreateTagAsync(bob, TagType.Person, "Bob");
        Assert.False(await service.MergeTagsAsync(aliceTag.Id, bobTag.Id, bob));

        // Alice's image and tag survive Bob's attempts.
        Assert.True(await database.Images.AnyAsync(value => value.Id == image.Id));
        Assert.True(await database.Tags.AnyAsync(value => value.Id == aliceTag.Id));
    }

    [Fact]
    public async Task Setting_image_tags_replaces_associations_and_rejects_foreign_tags()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");
        var other = await PostgresFixture.CreateUserAsync(database, $"other-{Guid.NewGuid():N}");

        var image = Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), owner, Now);
        database.Images.Add(image);
        await database.SaveChangesAsync();
        var person = await service.GetOrCreateTagAsync(owner, TagType.Person, "Mum");
        var place = await service.GetOrCreateTagAsync(owner, TagType.Place, "Home");
        var foreignTag = await service.GetOrCreateTagAsync(other, TagType.Topic, "Foreign");

        Assert.True(await service.SetImageTagsAsync(image.Id, owner, [person.Id, place.Id]));
        Assert.Equal(2, await database.ImageTags.CountAsync(link => link.ImageId == image.Id));

        // Replacing narrows to a single association.
        Assert.True(await service.SetImageTagsAsync(image.Id, owner, [person.Id]));
        Assert.Equal([person.Id], await database.ImageTags.Where(link => link.ImageId == image.Id).Select(link => link.TagId).ToListAsync());

        // A tag owned by someone else is refused.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetImageTagsAsync(image.Id, owner, [foreignTag.Id]));
    }

    [Fact]
    public async Task Reviewing_with_tag_values_creates_tags_replaces_links_and_marks_the_image()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");
        var image = Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), owner, Now);
        database.Images.Add(image);
        await database.SaveChangesAsync();

        Assert.True(await service.SetImageTagValuesAsync(image.Id, owner, [new TagValue(TagType.Person, "Ana"), new TagValue(TagType.Topic, "Summer")], markReviewed: true));
        database.ChangeTracker.Clear();

        var stored = await database.Images.SingleAsync(value => value.Id == image.Id);
        Assert.NotNull(stored.ReviewedAt);
        Assert.Equal(2, await database.ImageTags.CountAsync(link => link.ImageId == image.Id));
        Assert.Equal(2, await database.Tags.CountAsync(tag => tag.OwnerUserId == owner));
    }

    [Fact]
    public async Task Deleting_an_image_prunes_orphan_tags_keeps_shared_ones_and_removes_files()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var store = scope.ServiceProvider.GetRequiredService<IImageStore>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var first = Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), owner, Now);
        var second = Image.Create(new ImageValues(ShaB, "image/jpeg", 10, 10, 100, null), owner, Now);
        database.Images.AddRange(first, second);
        await database.SaveChangesAsync();

        var shared = await service.GetOrCreateTagAsync(owner, TagType.Topic, "Shared");
        var solo = await service.GetOrCreateTagAsync(owner, TagType.Topic, "Solo");
        await service.SetImageTagsAsync(first.Id, owner, [shared.Id, solo.Id]);
        await service.SetImageTagsAsync(second.Id, owner, [shared.Id]);

        // Give the first image stored derivatives to prove they are removed.
        await store.SaveAsync(owner, ShaA, ImageDerivative.Original, new MemoryStream([1, 2, 3]));
        await store.SaveAsync(owner, ShaA, ImageDerivative.Thumbnail, new MemoryStream([4, 5, 6]));

        Assert.True(await service.DeleteImageAsync(first.Id, owner));

        Assert.False(await database.Images.AnyAsync(value => value.Id == first.Id));
        Assert.False(await database.ImageTags.AnyAsync(link => link.ImageId == first.Id));
        Assert.False(await database.Tags.AnyAsync(tag => tag.Id == solo.Id)); // Orphaned → pruned.
        Assert.True(await database.Tags.AnyAsync(tag => tag.Id == shared.Id)); // Still on the second image.
        Assert.True(await database.Images.AnyAsync(value => value.Id == second.Id));
        Assert.False(await store.ExistsAsync(owner, ShaA, ImageDerivative.Original));
        Assert.False(await store.ExistsAsync(owner, ShaA, ImageDerivative.Thumbnail));
    }

    [Fact]
    public async Task Merging_tags_repoints_associations_drops_duplicates_and_deletes_the_source()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var onlySource = Image.Create(new ImageValues(ShaA, "image/jpeg", 10, 10, 100, null), owner, Now);
        var both = Image.Create(new ImageValues(ShaB, "image/jpeg", 10, 10, 100, null), owner, Now);
        database.Images.AddRange(onlySource, both);
        await database.SaveChangesAsync();

        var source = await service.GetOrCreateTagAsync(owner, TagType.Person, "Bob");
        var target = await service.GetOrCreateTagAsync(owner, TagType.Person, "Robert");
        await service.SetImageTagsAsync(onlySource.Id, owner, [source.Id]);
        await service.SetImageTagsAsync(both.Id, owner, [source.Id, target.Id]);

        Assert.True(await service.MergeTagsAsync(source.Id, target.Id, owner));

        Assert.False(await database.Tags.AnyAsync(tag => tag.Id == source.Id)); // Source deleted.
        // Every image formerly tagged with the source now carries exactly the target, with no duplicate join rows.
        Assert.Equal([target.Id], await database.ImageTags.Where(link => link.ImageId == onlySource.Id).Select(link => link.TagId).ToListAsync());
        Assert.Equal([target.Id], await database.ImageTags.Where(link => link.ImageId == both.Id).Select(link => link.TagId).ToListAsync());
    }

    [Fact]
    public async Task Merging_tags_of_different_types_is_refused()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var person = await service.GetOrCreateTagAsync(owner, TagType.Person, "Ana");
        var place = await service.GetOrCreateTagAsync(owner, TagType.Place, "Ana");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MergeTagsAsync(person.Id, place.Id, owner));
    }
}
