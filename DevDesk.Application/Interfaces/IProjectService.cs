using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ProjectDto> ArchiveAsync(Guid id, bool archive = true, CancellationToken ct = default);
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectListItemDto>> SearchAsync(string query, CancellationToken ct = default);
}
