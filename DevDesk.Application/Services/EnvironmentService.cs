using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class EnvironmentService(IDevDeskDbContext db, IClock clock) : IEnvironmentService
{
    public async Task<EnvironmentDto> CreateAsync(CreateEnvironmentRequest request, CancellationToken ct = default)
    {
        _ = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} was not found.");

        var now = clock.UtcNow;
        var environment = new ProjectEnvironment
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            EnvironmentType = request.EnvironmentType,
            BaseUrl = request.BaseUrl,
            DatabaseServer = request.DatabaseServer,
            DatabaseName = request.DatabaseName,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.ProjectEnvironments.Add(environment);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(environment.Id, ct);
    }

    public async Task<EnvironmentDto> UpdateAsync(Guid id, UpdateEnvironmentRequest request, CancellationToken ct = default)
    {
        var environment = await db.ProjectEnvironments.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Environment {id} was not found.");

        environment.Name = request.Name.Trim();
        environment.EnvironmentType = request.EnvironmentType;
        environment.BaseUrl = request.BaseUrl;
        environment.DatabaseServer = request.DatabaseServer;
        environment.DatabaseName = request.DatabaseName;
        environment.Notes = request.Notes;
        environment.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var environment = await db.ProjectEnvironments.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Environment {id} was not found.");
        db.ProjectEnvironments.Remove(environment);
        await db.SaveChangesAsync(ct);
    }

    public async Task<EnvironmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var environment = await DetailQuery().FirstOrDefaultAsync(e => e.Id == id, ct);
        return environment?.ToDto();
    }

    public async Task<IReadOnlyList<EnvironmentDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var items = await DetailQuery()
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.EnvironmentType)
            .ThenBy(e => e.Name)
            .ToListAsync(ct);
        return items.Select(e => e.ToDto()).ToList();
    }

    private IQueryable<ProjectEnvironment> DetailQuery() =>
        db.ProjectEnvironments.AsNoTracking().Include(e => e.Project);

    private async Task<EnvironmentDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var environment = await DetailQuery().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Environment {id} was not found.");
        return environment.ToDto();
    }
}
