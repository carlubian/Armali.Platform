using Segaris.Api.Composition;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Projects.Attention;
using Segaris.Api.Modules.Projects.Mutations;
using Segaris.Api.Modules.Projects.Persistence;
using Segaris.Api.Modules.Projects.Queries;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Projects;

/// <summary>
/// Business module that organises personal work into a navigable
/// <c>Program</c> → <c>Axis</c> → (<c>Project</c> | <c>Activity</c>) tree.
/// Wave 0 registers the module and freezes the public contracts (tree, project,
/// activity, risk, and attachment routes; the status and visibility vocabularies; the
/// risk-band thresholds; the unified-identifier format; DTOs; query contracts; stable
/// error codes; the attachment owner kind; the launcher attention key with its constant
/// non-attention contributor; and the Configuration-facing program/axis management and
/// reassignment contracts). Later waves add the hierarchy persistence and numbering,
/// program/axis management with reassignment, the project/activity tree and mutation
/// APIs, risks, attachments, and the frontend experience. Projects is an independent
/// business module: it may consume Configuration, Attachments, Identity, and platform
/// contracts but must not depend on any other business module, and no module depends on
/// Projects.
/// </summary>
internal sealed class ProjectsModule : ISegarisModule
{
    public string Name => "Projects";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, ProjectsModelContributor>();
        services.AddScoped<ProjectNumberAllocator>();
        services.AddScoped<ProjectItemWriteService>();
        services.AddScoped<ProjectsReadService>();
        services.AddScoped<ProjectsStructureManagementService>();
        services.AddScoped<ILauncherAttentionContributor, ProjectsAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapProjectsEndpoints();
    }
}
