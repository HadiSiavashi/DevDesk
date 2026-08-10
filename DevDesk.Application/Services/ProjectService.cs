using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class ProjectService(IDevDeskDbContext db, IClock clock) : IProjectService
{
    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color.Trim(),
            Icon = request.Icon,
            RepositoryUrl = request.RepositoryUrl,
            LocalPath = request.LocalPath,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(project.Id, ct);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.Color = string.IsNullOrWhiteSpace(request.Color) ? project.Color : request.Color.Trim();
        project.Icon = request.Icon;
        project.RepositoryUrl = request.RepositoryUrl;
        project.LocalPath = request.LocalPath;
        project.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProjectDto> ArchiveAsync(Guid id, bool archive = true, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");
        project.IsArchived = archive;
        project.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await DetailQuery().FirstOrDefaultAsync(p => p.Id == id, ct);
        return project?.ToDto();
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var query = ListQuery();
        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        var items = await query
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return items.Select(p => p.ToListItemDto()).ToList();
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var items = await ListQuery()
            .Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return items.Select(p => p.ToListItemDto()).ToList();
    }

    private IQueryable<Project> ListQuery() =>
        db.Projects.AsNoTracking().Include(p => p.Tasks);

    private IQueryable<Project> DetailQuery() =>
        db.Projects.AsNoTracking()
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Include(p => p.Environments);

    private async Task<ProjectDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var project = await DetailQuery().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");
        return project.ToDto();
    }
}
