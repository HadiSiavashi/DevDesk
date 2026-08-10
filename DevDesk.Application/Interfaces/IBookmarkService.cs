using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IBookmarkService
{
    Task<BookmarkDto> CreateAsync(CreateBookmarkRequest request, CancellationToken ct = default);
    Task<BookmarkDto> UpdateAsync(Guid id, UpdateBookmarkRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<BookmarkDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BookmarkDto>> GetAllAsync(string? category = null, CancellationToken ct = default);
    Task<IReadOnlyList<BookmarkDto>> SearchAsync(string query, CancellationToken ct = default);
}
