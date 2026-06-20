using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Mutations;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects;

internal static class ProjectsEndpoints
{
    public static void MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("projects", ProjectsApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/programs", ListProgramsAsync)
            .WithName("ListProjectPrograms")
            .WithSummary("Returns Project programs ordered by code")
            .Produces<IReadOnlyList<ProgramResponse>>();

        group.MapGet("/axes", ListAxesAsync)
            .WithName("ListProjectAxes")
            .WithSummary("Returns Project axes ordered by code")
            .Produces<IReadOnlyList<AxisResponse>>();

        var programs = group.MapGroup("/programs").RequireAuthorization(IdentityPolicies.Admin);
        programs.MapPost("", CreateProgramAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateProjectProgram").WithSummary("Creates a Project program").Produces<ProgramResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        programs.MapPut(ProjectsApiRoutes.ProgramById, UpdateProgramAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateProjectProgram").WithSummary("Updates a Project program name and code").Produces<ProgramResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        programs.MapGet(ProjectsApiRoutes.ProgramDeletionImpact, ProgramImpactAsync).WithName("GetProjectProgramDeletionImpact").WithSummary("Returns privacy-neutral Project program deletion impact").Produces<StructuralNodeDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        programs.MapDelete(ProjectsApiRoutes.ProgramById, DeleteProgramAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteProjectProgram").WithSummary("Deletes an empty Project program").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        programs.MapPost(ProjectsApiRoutes.ProgramReassignAndDelete, ReassignAndDeleteProgramAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReassignAndDeleteProjectProgram").WithSummary("Reassigns axes to another program and deletes the source program atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);

        var axes = group.MapGroup("/axes").RequireAuthorization(IdentityPolicies.Admin);
        axes.MapPost("", CreateAxisAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateProjectAxis").WithSummary("Creates a Project axis").Produces<AxisResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        axes.MapPut(ProjectsApiRoutes.AxisById, UpdateAxisAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateProjectAxis").WithSummary("Updates a Project axis name, code, and program").Produces<AxisResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        axes.MapGet(ProjectsApiRoutes.AxisDeletionImpact, AxisImpactAsync).WithName("GetProjectAxisDeletionImpact").WithSummary("Returns privacy-neutral Project axis deletion impact").Produces<StructuralNodeDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        axes.MapDelete(ProjectsApiRoutes.AxisById, DeleteAxisAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteProjectAxis").WithSummary("Deletes an empty Project axis").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        axes.MapPost(ProjectsApiRoutes.AxisReassignAndDelete, ReassignAndDeleteAxisAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReassignAndDeleteProjectAxis").WithSummary("Reassigns projects and activities to another axis and deletes the source axis atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static UserId Actor(ICurrentUser currentUser) => currentUser.UserId ?? throw ProjectsStructureProblem.ProgramNotFound();

    private static async Task<IResult> ListProgramsAsync(ProjectsStructureManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ListProgramsAsync(token));

    private static async Task<IResult> ListAxesAsync(ProjectsStructureManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ListAxesAsync(token));

    private static async Task<IResult> CreateProgramAsync(ProgramRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateProgramAsync(request, Actor(user), token);
        return TypedResults.Created($"/api/projects/programs/{value.Id}", value);
    }

    private static async Task<IResult> UpdateProgramAsync(int programId, ProgramRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateProgramAsync(programId, request, Actor(user), token));

    private static async Task<IResult> ProgramImpactAsync(int programId, ProjectsStructureManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ProgramImpactAsync(programId, token));

    private static async Task<IResult> DeleteProgramAsync(int programId, ProjectsStructureManagementService service, CancellationToken token)
    {
        await service.DeleteProgramAsync(programId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReassignAndDeleteProgramAsync(int programId, StructuralNodeReassignmentRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReassignAndDeleteProgramAsync(programId, request, Actor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateAxisAsync(AxisRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAxisAsync(request, Actor(user), token);
        return TypedResults.Created($"/api/projects/axes/{value.Id}", value);
    }

    private static async Task<IResult> UpdateAxisAsync(int axisId, AxisRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAxisAsync(axisId, request, Actor(user), token));

    private static async Task<IResult> AxisImpactAsync(int axisId, ProjectsStructureManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.AxisImpactAsync(axisId, token));

    private static async Task<IResult> DeleteAxisAsync(int axisId, ProjectsStructureManagementService service, CancellationToken token)
    {
        await service.DeleteAxisAsync(axisId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReassignAndDeleteAxisAsync(int axisId, StructuralNodeReassignmentRequest request, ProjectsStructureManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReassignAndDeleteAxisAsync(axisId, request, Actor(user), token);
        return TypedResults.NoContent();
    }
}
