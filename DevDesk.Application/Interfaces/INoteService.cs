using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface INoteService
{
    Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken ct = default);
    Task<NoteDto> UpdateAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<NoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<NoteDto>> GetAllAsync(bool knowledgeBaseOnly = false, CancellationToken ct = default);
    Task<IReadOnlyList<NoteDto>> SearchAsync(string query, CancellationToken ct = default);
}
