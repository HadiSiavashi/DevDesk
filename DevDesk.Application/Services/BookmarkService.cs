using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class BookmarkService(IDevDeskDbContext db, IClock clock) : IBookmarkService
{
    public async Task<BookmarkDto> CreateAsync(CreateBookmarkRequest request, CancellationToken ct = default)
    {
        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Url = request.Url.Trim(),
            Description = request.Description,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Tools" : request.Category.Trim(),
            ProjectId = request.ProjectId,
            IsFavorite = request.IsFavorite,
            CreatedAt = clock.UtcNow
        };
        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(bookmark.Id, ct);
    }

    public async Task<BookmarkDto> UpdateAsync(Guid id, UpdateBookmarkRequest request, CancellationToken ct = default)
    {
        var bookmark = await db.Bookmarks.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Bookmark {id} was not found.");

        bookmark.Title = request.Title.Trim();
        bookmark.Url = request.Url.Trim();
        bookmark.Description = request.Description;
        bookmark.Category = string.IsNullOrWhiteSpace(request.Category) ? bookmark.Category : request.Category.Trim();
        bookmark.ProjectId = request.ProjectId;
        bookmark.IsFavorite = request.IsFavorite;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bookmark = await db.Bookmarks.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Bookmark {id} was not found.");
        db.Bookmarks.Remove(bookmark);
        await db.SaveChangesAsync(ct);
    }

    public async Task<BookmarkDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bookmark = await DetailQuery().FirstOrDefaultAsync(b => b.Id == id, ct);
        return bookmark?.ToDto();
    }

    public async Task<IReadOnlyList<BookmarkDto>> GetAllAsync(string? category = null, CancellationToken ct = default)
    {
        var query = DetailQuery();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        var items = await query
            .OrderByDescending(b => b.IsFavorite)
            .ThenBy(b => b.Title)
            .ToListAsync(ct);
        return items.Select(b => b.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<BookmarkDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var items = await DetailQuery()
            .Where(b => b.Title.Contains(term) || b.Url.Contains(term) || (b.Description != null && b.Description.Contains(term)))
            .OrderBy(b => b.Title)
            .Take(100)
            .ToListAsync(ct);
        return items.Select(b => b.ToDto()).ToList();
    }

    private IQueryable<Bookmark> DetailQuery() =>
        db.Bookmarks.AsNoTracking().Include(b => b.Project);

    private async Task<BookmarkDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var bookmark = await DetailQuery().FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Bookmark {id} was not found.");
        return bookmark.ToDto();
    }
}
