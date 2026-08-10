using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface ISnippetService
{
    Task<CodeSnippetDto> CreateAsync(CreateSnippetRequest request, CancellationToken ct = default);
    Task<CodeSnippetDto> UpdateAsync(Guid id, UpdateSnippetRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CodeSnippetDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CodeSnippetDto>> GetAllAsync(string? language = null, CancellationToken ct = default);
    Task<IReadOnlyList<CodeSnippetDto>> SearchAsync(string query, CancellationToken ct = default);
}
