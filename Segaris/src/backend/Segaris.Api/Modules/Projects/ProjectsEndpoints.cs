using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Api.Modules.Projects.Mutations;
using Segaris.Api.Modules.Projects.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects;

internal static class ProjectsEndpoints
{
    public static void MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("projects", ProjectsApiRoutes.Tag)
            .RequireAuthorization();

        MapTreeEndpoints(group);
        MapProjectEndpoints(group);
        MapActivityEndpoints(group);
        MapStructureEndpoints(group);
    }

    private static void MapTreeEndpoints(RouteGroupBuilder group)
    {
        var tree = group.MapGroup("/tree");
        tree.MapGet("/programs", ListTreeProgramsAsync)
            .WithName("ListProjectTreePrograms")
            .WithSummary("Returns Project tree programs ordered by code")
            .Produces<IReadOnlyList<ProgramNodeResponse>>();

        tree.MapGet("/programs/{programId:int}/axes", ListTreeAxesAsync)
            .WithName("ListProjectTreeAxes")
            .WithSummary("Returns Project tree axes for a program ordered by code")
            .Produces<IReadOnlyList<AxisNodeResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        tree.MapGet("/axes/{axisId:int}/items", ListTreeItemsAsync)
            .WithName("ListProjectTreeItems")
            .WithSummary("Returns accessible projects and activities for an axis ordered by number")
            .Produces<IReadOnlyList<ProjectTreeItemResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapProjectEndpoints(RouteGroupBuilder group)
    {
        var projects = group.MapGroup("/projects");
        projects.MapPost("", CreateProjectAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateProject")
            .WithSummary("Creates a Project under an axis")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapGet("/{projectId:int}", GetProjectAsync)
            .WithName("GetProject")
            .WithSummary("Returns an accessible Project")
            .Produces<ProjectResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapPut("/{projectId:int}", UpdateProjectAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateProject")
            .WithSummary("Updates an accessible Project")
            .Produces<ProjectResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapDelete("/{projectId:int}", DeleteProjectAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteProject")
            .WithSummary("Deletes an accessible Project")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapGet(ProjectsApiRoutes.ProjectRisks, ListProjectRisksAsync)
            .WithName("ListProjectRisks")
            .WithSummary("Returns risks owned by an accessible Project")
            .Produces<IReadOnlyList<ProjectRiskResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapPost(ProjectsApiRoutes.ProjectRisks, CreateProjectRiskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateProjectRisk")
            .WithSummary("Creates a risk owned by an accessible Project")
            .Produces<ProjectRiskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapGet(ProjectsApiRoutes.ProjectRiskById, GetProjectRiskAsync)
            .WithName("GetProjectRisk")
            .WithSummary("Returns a Project risk")
            .Produces<ProjectRiskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapPut(ProjectsApiRoutes.ProjectRiskById, UpdateProjectRiskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateProjectRisk")
            .WithSummary("Updates a Project risk")
            .Produces<ProjectRiskResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapDelete(ProjectsApiRoutes.ProjectRiskById, DeleteProjectRiskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteProjectRisk")
            .WithSummary("Deletes a Project risk")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapActivityEndpoints(RouteGroupBuilder group)
    {
        var activities = group.MapGroup("/activities");
        activities.MapPost("", CreateActivityAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateProjectActivity")
            .WithSummary("Creates a Project activity under an axis")
            .Produces<ActivityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        activities.MapGet("/{activityId:int}", GetActivityAsync)
            .WithName("GetProjectActivity")
            .WithSummary("Returns an accessible Project activity")
            .Produces<ActivityResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        activities.MapPut("/{activityId:int}", UpdateActivityAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateProjectActivity")
            .WithSummary("Updates an accessible Project activity")
            .Produces<ActivityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        activities.MapDelete("/{activityId:int}", DeleteActivityAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteProjectActivity")
            .WithSummary("Deletes an accessible Project activity")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapStructureEndpoints(RouteGroupBuilder group)
    {
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

    private static async Task<IResult> ListTreeProgramsAsync(ProjectsReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListProgramsAsync(token));

    private static async Task<IResult> ListTreeAxesAsync(int programId, ProjectsReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListAxesByProgramAsync(programId, token));

    private static async Task<IResult> ListTreeItemsAsync(int axisId, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await read.ListItemsByAxisAsync(axisId, userId, token));
    }

    private static async Task<IResult> GetProjectAsync(int projectId, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return await read.GetProjectAsync(projectId, userId, token) is { } project
            ? TypedResults.Ok(project)
            : throw ProjectsProblem.ProjectNotFound();
    }

    private static async Task<IResult> CreateProjectAsync(CreateProjectRequest request, ProjectItemWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int projectId;
        try
        {
            projectId = await write.CreateProjectAsync(request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromProjectValidation(exception);
        }

        var project = await read.GetProjectAsync(projectId, userId, token);
        return TypedResults.Created($"/api/projects/projects/{projectId}", project);
    }

    private static async Task<IResult> UpdateProjectAsync(int projectId, UpdateProjectRequest request, ProjectItemWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateProjectAsync(projectId, request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromProjectValidation(exception);
        }

        if (!updated)
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        return TypedResults.Ok(await read.GetProjectAsync(projectId, userId, token));
    }

    private static async Task<IResult> DeleteProjectAsync(int projectId, ProjectItemWriteService write, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await write.DeleteProjectAsync(projectId, userId, token))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListProjectRisksAsync(int projectId, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await read.ListRisksAsync(projectId, userId, token));
    }

    private static async Task<IResult> GetProjectRiskAsync(int projectId, int riskId, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return await read.GetRiskAsync(projectId, riskId, userId, token) is { } risk
            ? TypedResults.Ok(risk)
            : throw ProjectsProblem.RiskNotFound();
    }

    private static async Task<IResult> CreateProjectRiskAsync(int projectId, ProjectRiskRequest request, ProjectRiskWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int riskId;
        try
        {
            riskId = await write.CreateRiskAsync(projectId, request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromRiskValidation(exception);
        }

        var risk = await read.GetRiskAsync(projectId, riskId, userId, token);
        return TypedResults.Created($"/api/projects/projects/{projectId}/risks/{riskId}", risk);
    }

    private static async Task<IResult> UpdateProjectRiskAsync(int projectId, int riskId, ProjectRiskRequest request, ProjectRiskWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateRiskAsync(projectId, riskId, request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromRiskValidation(exception);
        }

        if (!updated)
        {
            throw ProjectsProblem.RiskNotFound();
        }

        return TypedResults.Ok(await read.GetRiskAsync(projectId, riskId, userId, token));
    }

    private static async Task<IResult> DeleteProjectRiskAsync(int projectId, int riskId, ProjectRiskWriteService write, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await write.DeleteRiskAsync(projectId, riskId, userId, token))
        {
            throw ProjectsProblem.RiskNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetActivityAsync(int activityId, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return await read.GetActivityAsync(activityId, userId, token) is { } activity
            ? TypedResults.Ok(activity)
            : throw ProjectsProblem.ActivityNotFound();
    }

    private static async Task<IResult> CreateActivityAsync(CreateActivityRequest request, ProjectItemWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int activityId;
        try
        {
            activityId = await write.CreateActivityAsync(request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromActivityValidation(exception);
        }

        var activity = await read.GetActivityAsync(activityId, userId, token);
        return TypedResults.Created($"/api/projects/activities/{activityId}", activity);
    }

    private static async Task<IResult> UpdateActivityAsync(int activityId, UpdateActivityRequest request, ProjectItemWriteService write, ProjectsReadService read, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateActivityAsync(activityId, request, userId, token);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsProblem.FromActivityValidation(exception);
        }

        if (!updated)
        {
            throw ProjectsProblem.ActivityNotFound();
        }

        return TypedResults.Ok(await read.GetActivityAsync(activityId, userId, token));
    }

    private static async Task<IResult> DeleteActivityAsync(int activityId, ProjectItemWriteService write, ICurrentUser currentUser, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await write.DeleteActivityAsync(activityId, userId, token))
        {
            throw ProjectsProblem.ActivityNotFound();
        }

        return TypedResults.NoContent();
    }

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
