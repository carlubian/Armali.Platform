using Blackwing.Persistence.Content;
using Blackwing.Persistence.Uploads;
using Blackwing.Shared.Ownership;

namespace Blackwing.ArchitectureTests;

/// <summary>
/// A nominal check, on purpose. <see cref="OwnershipTests"/> proves the mechanism — every owned
/// entity in the model has a filter — but a generic rule cannot notice a content entity that was
/// never made owned in the first place. This one names the four that must be, so none of them can
/// quietly fall outside the privacy perimeter.
/// </summary>
public sealed class ContentOwnershipTests
{
    public static TheoryData<Type> ContentEntities() => new()
    {
        typeof(Image),
        typeof(Tag),
        typeof(ImageTag),
        typeof(UploadJob),
    };

    [Theory]
    [MemberData(nameof(ContentEntities))]
    public void Every_content_entity_is_owned_by_a_user(Type entity)
    {
        Assert.True(
            typeof(IOwnedByUser).IsAssignableFrom(entity),
            $"{entity.Name} must implement {nameof(IOwnedByUser)}.");
    }

    [Theory]
    [MemberData(nameof(ContentEntities))]
    public void Every_content_entity_is_mapped_by_the_model(Type entity)
    {
        var mapped = ProductionModel.Value.GetEntityTypes().Select(entityType => entityType.ClrType);

        Assert.Contains(entity, mapped);
    }

    [Theory]
    [MemberData(nameof(ContentEntities))]
    public void Every_content_entity_is_scoped_by_a_query_filter(Type entity)
    {
        var entityType = ProductionModel.Value.FindEntityType(entity);

        Assert.NotNull(entityType);
        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
    }
}
