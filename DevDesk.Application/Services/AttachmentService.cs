using System.IO;
using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class AttachmentService(IDevDeskDbContext db, IClock clock) : IAttachmentService
{
    private static string AttachmentsRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevDesk", "Attachments");

    public async Task<IReadOnlyList<AttachmentDto>> GetForTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var items = await db.Attachments.AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetForNoteAsync(Guid noteId, CancellationToken ct = default)
    {
        var items = await db.Attachments.AsNoTracking()
            .Where(a => a.NoteId == noteId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public Task<AttachmentDto> AddForTaskAsync(Guid taskId, string sourceFilePath, CancellationToken ct = default)
        => AddAsync(sourceFilePath, taskId: taskId, noteId: null, ct);

    public Task<AttachmentDto> AddForNoteAsync(Guid noteId, string sourceFilePath, CancellationToken ct = default)
        => AddAsync(sourceFilePath, taskId: null, noteId: noteId, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Attachment {id} was not found.");

        var path = attachment.FilePath;
        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync(ct);

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort — DB row already removed.
        }
    }

    private async Task<AttachmentDto> AddAsync(string sourceFilePath, Guid? taskId, Guid? noteId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            throw new FileNotFoundException("Attachment source file was not found.", sourceFilePath);

        // Basic path validation — reject traversal into unexpected locations after copy target.
        var fileName = Path.GetFileName(sourceFilePath);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid file name.", nameof(sourceFilePath));

        Directory.CreateDirectory(AttachmentsRoot);
        var id = Guid.NewGuid();
        var destName = $"{id:N}_{fileName}";
        var destPath = Path.Combine(AttachmentsRoot, destName);
        File.Copy(sourceFilePath, destPath, overwrite: false);

        var info = new FileInfo(destPath);
        var now = clock.UtcNow;
        var entity = new Attachment
        {
            Id = id,
            FileName = fileName,
            FilePath = destPath,
            ContentType = GuessContentType(fileName),
            Size = info.Length,
            TaskId = taskId,
            NoteId = noteId,
            CreatedAt = now
        };

        db.Attachments.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" or ".md" or ".log" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".cs" or ".sql" or ".js" or ".ts" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };

    private static AttachmentDto ToDto(Attachment a) => new()
    {
        Id = a.Id,
        FileName = a.FileName,
        FilePath = a.FilePath,
        ContentType = a.ContentType,
        Size = a.Size,
        TaskId = a.TaskId,
        NoteId = a.NoteId,
        CreatedAt = a.CreatedAt
    };
}
