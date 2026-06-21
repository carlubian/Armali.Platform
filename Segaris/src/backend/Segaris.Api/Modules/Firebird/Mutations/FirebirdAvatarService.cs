using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Firebird.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Mutations;

internal sealed class FirebirdAvatarService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<PersonAvatarResponse?> PutAsync(
        int personId,
        string fileName,
        string contentType,
        Stream content,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var person = await database.Set<Person>()
            .Where(PersonPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == personId)
            .SingleOrDefaultAsync(cancellationToken);
        if (person is null)
        {
            return null;
        }

        if (!FirebirdAttachments.IsAvatarContentType(contentType))
        {
            throw FirebirdSubResourceProblem.AvatarInvalid("contentType", "The avatar must be an image.");
        }

        var owner = FirebirdAttachments.PersonOwner(personId);
        var previous = await attachments.FindByOwnerAsync(owner, cancellationToken);

        AttachmentDescriptor created;
        try
        {
            created = await attachments.CreateAsync(
                new(owner, fileName, contentType, content),
                actorId,
                cancellationToken);
        }
        catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            throw FirebirdSubResourceProblem.AvatarInvalid("file", exception.Message, exception.Errors);
        }

        person.SetAvatarAttachment(created.Id.Value, actorId, clock.UtcNow);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await attachments.DeleteAsync(created.Id, owner, cancellationToken);
            throw;
        }

        if (previous is not null)
        {
            await attachments.DeleteAsync(previous.Id, owner, cancellationToken);
        }

        return FirebirdAvatarResponseFactory.Avatar(personId, created.Id.Value);
    }

    public async Task<AttachmentDownload?> OpenReadAsync(
        int personId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var attachmentId = await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.AccessibleTo(actorId))
            .Where(person => person.Id == personId)
            .Select(person => person.AvatarAttachmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (attachmentId is null)
        {
            return null;
        }

        return await attachments.OpenReadAsync(
            new(attachmentId.Value),
            FirebirdAttachments.PersonOwner(personId),
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int personId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var person = await database.Set<Person>()
            .Where(PersonPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == personId)
            .SingleOrDefaultAsync(cancellationToken);
        if (person?.AvatarAttachmentId is null)
        {
            return false;
        }

        var attachmentId = new AttachmentId(person.AvatarAttachmentId.Value);
        person.SetAvatarAttachment(null, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);

        return await attachments.DeleteAsync(
            attachmentId,
            FirebirdAttachments.PersonOwner(personId),
            cancellationToken);
    }
}
