using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IEnvironmentService
{
    Task<EnvironmentDto> CreateAsync(CreateEnvironmentRequest request, CancellationToken ct = default);
    Task<EnvironmentDto> UpdateAsync(Guid id, UpdateEnvironmentRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<EnvironmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EnvironmentDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
}
