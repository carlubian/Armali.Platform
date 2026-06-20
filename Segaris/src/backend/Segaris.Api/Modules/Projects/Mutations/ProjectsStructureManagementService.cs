using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Projects.Mutations;

internal sealed class ProjectsStructureManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<IReadOnlyList<ProgramResponse>> ListProgramsAsync(CancellationToken cancellationToken) =>
        await database.Set<ProjectProgram>()
            .AsNoTracking()
            .OrderBy(program => program.Code)
            .ThenBy(program => program.Id)
            .Select(program => new ProgramResponse(program.Id, program.Code, program.Name))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<AxisResponse>> ListAxesAsync(CancellationToken cancellationToken) =>
        await database.Set<ProjectAxis>()
            .AsNoTracking()
            .OrderBy(axis => axis.Code)
            .ThenBy(axis => axis.Id)
            .Select(axis => new AxisResponse(axis.Id, axis.Code, axis.Name, axis.ProgramId))
            .ToArrayAsync(cancellationToken);

    public async Task<ProgramResponse> CreateProgramAsync(ProgramRequest request, UserId actor, CancellationToken cancellationToken)
    {
        ProjectProgram program;
        try
        {
            program = ProjectProgram.Create(request.Name, request.Code, actor, clock.UtcNow);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsStructureProblem.FromProgramValidation(exception);
        }

        await EnsureUniqueProgramCodeAsync(null, program.Code, cancellationToken);
        database.Add(program);
        await SaveProgramAsync(cancellationToken);
        return ToResponse(program);
    }

    public async Task<ProgramResponse> UpdateProgramAsync(int programId, ProgramRequest request, UserId actor, CancellationToken cancellationToken)
    {
        var program = await FindProgramAsync(programId, cancellationToken);
        try
        {
            program.Update(request.Name, request.Code, actor, clock.UtcNow);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsStructureProblem.FromProgramValidation(exception);
        }

        await EnsureUniqueProgramCodeAsync(programId, program.Code, cancellationToken);
        await SaveProgramAsync(cancellationToken);
        return ToResponse(program);
    }

    public async Task<AxisResponse> CreateAxisAsync(AxisRequest request, UserId actor, CancellationToken cancellationToken)
    {
        if (!await ProgramExistsAsync(request.ProgramId, cancellationToken))
        {
            throw ProjectsStructureProblem.ProgramNotFound();
        }

        ProjectAxis axis;
        try
        {
            axis = ProjectAxis.Create(request.ProgramId, request.Name, request.Code, actor, clock.UtcNow);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsStructureProblem.FromAxisValidation(exception);
        }

        await EnsureUniqueAxisCodeAsync(null, axis.Code, cancellationToken);
        database.Add(axis);
        await SaveAxisAsync(cancellationToken);
        return ToResponse(axis);
    }

    public async Task<AxisResponse> UpdateAxisAsync(int axisId, AxisRequest request, UserId actor, CancellationToken cancellationToken)
    {
        if (!await ProgramExistsAsync(request.ProgramId, cancellationToken))
        {
            throw ProjectsStructureProblem.ProgramNotFound();
        }

        var axis = await FindAxisAsync(axisId, cancellationToken);
        try
        {
            axis.Update(request.ProgramId, request.Name, request.Code, actor, clock.UtcNow);
        }
        catch (ProjectsValidationException exception)
        {
            throw ProjectsStructureProblem.FromAxisValidation(exception);
        }

        await EnsureUniqueAxisCodeAsync(axisId, axis.Code, cancellationToken);
        await SaveAxisAsync(cancellationToken);
        return ToResponse(axis);
    }

    public async Task<StructuralNodeDeletionImpactResponse> ProgramImpactAsync(int programId, CancellationToken cancellationToken)
    {
        await FindProgramAsync(programId, cancellationToken, tracked: false);
        var childCount = await database.Set<ProjectAxis>().CountAsync(axis => axis.ProgramId == programId, cancellationToken);
        var hasTarget = await database.Set<ProjectProgram>().AnyAsync(program => program.Id != programId, cancellationToken);
        return new(childCount, hasTarget);
    }

    public async Task<StructuralNodeDeletionImpactResponse> AxisImpactAsync(int axisId, CancellationToken cancellationToken)
    {
        await FindAxisAsync(axisId, cancellationToken, tracked: false);
        var childCount =
            await database.Set<Project>().CountAsync(project => project.AxisId == axisId, cancellationToken)
            + await database.Set<Activity>().CountAsync(activity => activity.AxisId == axisId, cancellationToken);
        var hasTarget = await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id != axisId, cancellationToken);
        return new(childCount, hasTarget);
    }

    public async Task DeleteProgramAsync(int programId, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var program = await FindProgramAsync(programId, cancellationToken);
        if (await database.Set<ProjectAxis>().AnyAsync(axis => axis.ProgramId == programId, cancellationToken))
        {
            throw ProjectsStructureProblem.ReassignmentRequired();
        }

        database.Remove(program);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAxisAsync(int axisId, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var axis = await FindAxisAsync(axisId, cancellationToken);
        if (await AxisHasChildrenAsync(axisId, cancellationToken))
        {
            throw ProjectsStructureProblem.ReassignmentRequired();
        }

        database.Remove(axis);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ReassignAndDeleteProgramAsync(
        int programId,
        StructuralNodeReassignmentRequest request,
        UserId actor,
        CancellationToken cancellationToken)
    {
        if (request.TargetNodeId <= 0 || request.TargetNodeId == programId)
        {
            throw ProjectsStructureProblem.InvalidReassignmentTarget();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var source = await FindProgramAsync(programId, cancellationToken);
        if (!await database.Set<ProjectProgram>().AnyAsync(program => program.Id != programId, cancellationToken))
        {
            throw ProjectsStructureProblem.NoCompatibleTarget();
        }

        if (!await database.Set<ProjectProgram>().AnyAsync(program => program.Id == request.TargetNodeId, cancellationToken))
        {
            throw ProjectsStructureProblem.InvalidReassignmentTarget();
        }

        var axes = await database.Set<ProjectAxis>()
            .Where(axis => axis.ProgramId == programId)
            .ToArrayAsync(cancellationToken);
        var now = clock.UtcNow;
        foreach (var axis in axes)
        {
            axis.ReplaceProgram(request.TargetNodeId, actor, now);
        }

        database.Remove(source);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ReassignAndDeleteAxisAsync(
        int axisId,
        StructuralNodeReassignmentRequest request,
        UserId actor,
        CancellationToken cancellationToken)
    {
        if (request.TargetNodeId <= 0 || request.TargetNodeId == axisId)
        {
            throw ProjectsStructureProblem.InvalidReassignmentTarget();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var source = await FindAxisAsync(axisId, cancellationToken);
        if (!await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id != axisId, cancellationToken))
        {
            throw ProjectsStructureProblem.NoCompatibleTarget();
        }

        if (!await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id == request.TargetNodeId, cancellationToken))
        {
            throw ProjectsStructureProblem.InvalidReassignmentTarget();
        }

        var now = clock.UtcNow;
        foreach (var project in await database.Set<Project>().Where(project => project.AxisId == axisId).ToArrayAsync(cancellationToken))
        {
            project.ReplaceAxis(request.TargetNodeId, actor, now);
        }

        foreach (var activity in await database.Set<Activity>().Where(activity => activity.AxisId == axisId).ToArrayAsync(cancellationToken))
        {
            activity.ReplaceAxis(request.TargetNodeId, actor, now);
        }

        database.Remove(source);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task<bool> ProgramExistsAsync(int programId, CancellationToken cancellationToken) =>
        programId > 0
            ? database.Set<ProjectProgram>().AnyAsync(program => program.Id == programId, cancellationToken)
            : Task.FromResult(false);

    private async Task<ProjectProgram> FindProgramAsync(int programId, CancellationToken cancellationToken, bool tracked = true)
    {
        IQueryable<ProjectProgram> query = database.Set<ProjectProgram>();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(program => program.Id == programId, cancellationToken)
            ?? throw ProjectsStructureProblem.ProgramNotFound();
    }

    private async Task<ProjectAxis> FindAxisAsync(int axisId, CancellationToken cancellationToken, bool tracked = true)
    {
        IQueryable<ProjectAxis> query = database.Set<ProjectAxis>();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(axis => axis.Id == axisId, cancellationToken)
            ?? throw ProjectsStructureProblem.AxisNotFound();
    }

    private async Task<bool> AxisHasChildrenAsync(int axisId, CancellationToken cancellationToken) =>
        await database.Set<Project>().AnyAsync(project => project.AxisId == axisId, cancellationToken)
        || await database.Set<Activity>().AnyAsync(activity => activity.AxisId == axisId, cancellationToken);

    private async Task EnsureUniqueProgramCodeAsync(int? programId, string code, CancellationToken cancellationToken)
    {
        if (await database.Set<ProjectProgram>().AnyAsync(program => program.Code == code && program.Id != programId, cancellationToken))
        {
            throw ProjectsStructureProblem.ProgramDuplicateCode();
        }
    }

    private async Task EnsureUniqueAxisCodeAsync(int? axisId, string code, CancellationToken cancellationToken)
    {
        if (await database.Set<ProjectAxis>().AnyAsync(axis => axis.Code == code && axis.Id != axisId, cancellationToken))
        {
            throw ProjectsStructureProblem.AxisDuplicateCode();
        }
    }

    private async Task SaveProgramAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ProjectsStructureProblem.ProgramDuplicateCode();
        }
    }

    private async Task SaveAxisAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ProjectsStructureProblem.AxisDuplicateCode();
        }
    }

    private static ProgramResponse ToResponse(ProjectProgram program) => new(program.Id, program.Code, program.Name);

    private static AxisResponse ToResponse(ProjectAxis axis) => new(axis.Id, axis.Code, axis.Name, axis.ProgramId);
}
