using Segaris.Api.Composition;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Processes.Attention;
using Segaris.Api.Modules.Processes.Mutations;
using Segaris.Api.Modules.Processes.Persistence;
using Segaris.Api.Modules.Processes.Queries;
using Segaris.Api.Modules.Processes.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Business module that manages sequential procedures the household carries out step by
/// step in order, by a target date. Wave 0 registers the module and freezes the public
/// contracts (process, step, attachment, and category routes; the derived status
/// vocabulary and the <c>Cancelled</c> override; the step execution states; the
/// visibility vocabulary; the frontier and contiguity rules as a documented domain
/// contract; DTOs; query contracts; stable error codes; the attachment owner kind; the
/// launcher attention key with its constant non-attention contributor; and the
/// Configuration-facing <c>ProcessCategory</c> contracts). Later waves add the
/// persistence model and the module-owned category catalogue, the process read and
/// mutation APIs, the editable step list and sequential execution, attachments, launcher
/// attention, and the frontend experience. Processes is an independent business module:
/// it may consume Configuration, Attachments, Identity, and platform contracts but must
/// not depend on any other business module, and no module depends on Processes.
/// </summary>
internal sealed class ProcessesModule : ISegarisModule
{
    public string Name => "Processes";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, ProcessesModelContributor>();
        services.AddScoped<ProcessesSeeder>();
        services.AddScoped<ProcessCategoryReadService>();
        services.AddScoped<ProcessCategoryManagementService>();
        services.AddScoped<ProcessReadService>();
        services.AddScoped<ProcessWriteService>();
        services.AddScoped<ILauncherAttentionContributor, ProcessesAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapProcessesEndpoints();
    }
}
