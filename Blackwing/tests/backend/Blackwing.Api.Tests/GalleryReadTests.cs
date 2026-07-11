using Blackwing.Api.Gallery;
using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Api.Tests;

/// <summary>
/// Exercises the gallery read model directly against Postgres: the generated
/// effective-date ordering (capture date, upload-time fallback), stable keyset
/// pagination, closed AND tag filtering, the review-status views and facets.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class GalleryReadTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day3 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Effective_date_falls_back_to_upload_time_and_orders_newest_first()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryReadService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        // Uploaded together on Day1, but captured on different days; one has no capture date.
        var oldest = Add(database, owner, Sha(), captured: Day1, uploaded: Day1);
        var noCapture = Add(database, owner, Sha(), captured: null, uploaded: Day2); // Effective = Day2.
        var newest = Add(database, owner, Sha(), captured: Day3, uploaded: Day1);
        await database.SaveChangesAsync();

        var page = await gallery.BrowseAsync(owner, [], ReviewFilter.All, cursor: null, limit: 10);
        Assert.Equal([newest.Id, noCapture.Id, oldest.Id], page.Items.Select(item => item.Id).ToList());
        Assert.Null(page.NextCursor);
        // The fallback surfaces through the generated column.
        Assert.Equal(Day2, page.Items.Single(item => item.Id == noCapture.Id).EffectiveCapturedAt);
    }

    [Fact]
    public async Task Keyset_pagination_walks_the_whole_collection_without_gaps_or_repeats()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryReadService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var expected = new List<Guid>();
        for (var index = 0; index < 5; index++)
            expected.Add(Add(database, owner, Sha(), captured: Day1.AddMinutes(index), uploaded: Day1).Id);
        await database.SaveChangesAsync();
        expected.Reverse(); // Newest first.

        var walked = new List<Guid>();
        string? cursor = null;
        do
        {
            var page = await gallery.BrowseAsync(owner, [], ReviewFilter.All, cursor, limit: 2);
            walked.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(expected, walked);
    }

    [Fact]
    public async Task Tag_filters_combine_with_AND_semantics()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryReadService>();
        var mutations = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var both = Add(database, owner, Sha(), captured: Day3, uploaded: Day1);
        var onlyPerson = Add(database, owner, Sha(), captured: Day2, uploaded: Day1);
        await database.SaveChangesAsync();

        var person = await mutations.GetOrCreateTagAsync(owner, TagType.Person, "Ada");
        var place = await mutations.GetOrCreateTagAsync(owner, TagType.Place, "Paris");
        await mutations.SetImageTagsAsync(both.Id, owner, [person.Id, place.Id]);
        await mutations.SetImageTagsAsync(onlyPerson.Id, owner, [person.Id]);

        var byPerson = await gallery.BrowseAsync(owner, [person.Id], ReviewFilter.All, null, 10);
        Assert.Equal([both.Id, onlyPerson.Id], byPerson.Items.Select(item => item.Id).ToList());

        // Requiring both tags returns only the image carrying every selected tag.
        var byBoth = await gallery.BrowseAsync(owner, [person.Id, place.Id], ReviewFilter.All, null, 10);
        Assert.Equal([both.Id], byBoth.Items.Select(item => item.Id).ToList());
    }

    [Fact]
    public async Task Review_filter_separates_pending_from_reviewed()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryReadService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var pending = Add(database, owner, Sha(), captured: Day2, uploaded: Day1);
        var reviewed = Add(database, owner, Sha(), captured: Day1, uploaded: Day1);
        reviewed.MarkReviewed(Day3);
        await database.SaveChangesAsync();

        Assert.Equal([pending.Id], (await gallery.BrowseAsync(owner, [], ReviewFilter.Pending, null, 10)).Items.Select(item => item.Id).ToList());
        Assert.Equal([reviewed.Id], (await gallery.BrowseAsync(owner, [], ReviewFilter.Reviewed, null, 10)).Items.Select(item => item.Id).ToList());
        Assert.Equal(2, (await gallery.BrowseAsync(owner, [], ReviewFilter.All, null, 10)).Items.Count);
    }

    [Fact]
    public async Task Facets_report_the_owners_tags_with_image_counts()
    {
        if (!fixture.Available) return;
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryReadService>();
        var mutations = scope.ServiceProvider.GetRequiredService<GalleryMutationService>();
        var owner = await PostgresFixture.CreateUserAsync(database, $"owner-{Guid.NewGuid():N}");

        var one = Add(database, owner, Sha(), captured: Day2, uploaded: Day1);
        var two = Add(database, owner, Sha(), captured: Day1, uploaded: Day1);
        await database.SaveChangesAsync();
        var person = await mutations.GetOrCreateTagAsync(owner, TagType.Person, "Ada");
        await mutations.SetImageTagsAsync(one.Id, owner, [person.Id]);
        await mutations.SetImageTagsAsync(two.Id, owner, [person.Id]);

        var facets = await gallery.TagFacetsAsync(owner);
        var facet = Assert.Single(facets);
        Assert.Equal("Person", facet.Type);
        Assert.Equal("Ada", facet.Value);
        Assert.Equal(2, facet.Count);
    }

    private static Image Add(BlackwingDbContext database, Guid owner, string sha, DateTimeOffset? captured, DateTimeOffset uploaded)
    {
        var image = Image.Create(new ImageValues(sha, "image/jpeg", 100, 80, 1000, captured), owner, uploaded);
        database.Images.Add(image);
        return image;
    }

    private static string Sha() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
}
