using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class NoteService(IDevDeskDbContext db, IClock clock) : INoteService
{
    public async Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Content = request.Content,
            ProjectId = request.ProjectId,
            IsPinned = request.IsPinned,
            IsKnowledgeBase = request.IsKnowledgeBase,
            KnowledgeCategory = request.KnowledgeCategory,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.TagNames is { Count: > 0 })
            await AttachTagsAsync(note, request.TagNames, ct);

        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(note.Id, ct);
    }

    public async Task<NoteDto> UpdateAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default)
    {
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Note {id} was not found.");

        note.Title = request.Title.Trim();
        note.Content = request.Content;
        note.ProjectId = request.ProjectId;
        note.IsPinned = request.IsPinned;
        note.IsKnowledgeBase = request.IsKnowledgeBase;
        note.KnowledgeCategory = request.KnowledgeCategory;
        note.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Note {id} was not found.");
        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
    }

    public async Task<NoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var note = await DetailQuery().FirstOrDefaultAsync(n => n.Id == id, ct);
        return note?.ToDto();
    }

    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(bool knowledgeBaseOnly = false, CancellationToken ct = default)
    {
        var query = DetailQuery();
        if (knowledgeBaseOnly)
            query = query.Where(n => n.IsKnowledgeBase);

        var items = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync(ct);
        return items.Select(n => n.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<NoteDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var items = await DetailQuery()
            .Where(n => n.Title.Contains(term) || n.Content.Contains(term))
            .OrderByDescending(n => n.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
        return items.Select(n => n.ToDto()).ToList();
    }

    private IQueryable<Note> DetailQuery() =>
        db.Notes.AsNoTracking()
            .Include(n => n.Project)
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag);

    private async Task<NoteDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var note = await DetailQuery().FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Note {id} was not found.");
        return note.ToDto();
    }

    private async Task AttachTagsAsync(Note note, IReadOnlyList<string> tagNames, CancellationToken ct)
    {
        foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name, ct);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = name };
                db.Tags.Add(tag);
            }

            note.NoteTags.Add(new NoteTag { NoteId = note.Id, TagId = tag.Id, Tag = tag });
        }
    }
}
