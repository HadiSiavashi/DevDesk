using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> GetForTaskAsync(Guid taskId, CancellationToken ct = default);
    Task<IReadOnlyList<AttachmentDto>> GetForNoteAsync(Guid noteId, CancellationToken ct = default);
    Task<AttachmentDto> AddForTaskAsync(Guid taskId, string sourceFilePath, CancellationToken ct = default);
    Task<AttachmentDto> AddForNoteAsync(Guid noteId, string sourceFilePath, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
