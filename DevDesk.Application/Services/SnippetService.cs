using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class SnippetService(IDevDeskDbContext db, IClock clock) : ISnippetService
{
    public async Task<CodeSnippetDto> CreateAsync(CreateSnippetRequest request, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "C#" : request.Language.Trim(),
            Code = request.Code,
            ProjectId = request.ProjectId,
            IsFavorite = request.IsFavorite,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.CodeSnippets.Add(snippet);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(snippet.Id, ct);
    }

    public async Task<CodeSnippetDto> UpdateAsync(Guid id, UpdateSnippetRequest request, CancellationToken ct = default)
    {
        var snippet = await db.CodeSnippets.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Snippet {id} was not found.");

        snippet.Title = request.Title.Trim();
        snippet.Description = request.Description;
        snippet.Language = string.IsNullOrWhiteSpace(request.Language) ? snippet.Language : request.Language.Trim();
        snippet.Code = request.Code;
        snippet.ProjectId = request.ProjectId;
        snippet.IsFavorite = request.IsFavorite;
        snippet.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var snippet = await db.CodeSnippets.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Snippet {id} was not found.");
        db.CodeSnippets.Remove(snippet);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CodeSnippetDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var snippet = await DetailQuery().FirstOrDefaultAsync(s => s.Id == id, ct);
        return snippet?.ToDto();
    }

    public async Task<IReadOnlyList<CodeSnippetDto>> GetAllAsync(string? language = null, CancellationToken ct = default)
    {
        var query = DetailQuery();
        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(s => s.Language == language);

        var items = await query
            .OrderByDescending(s => s.IsFavorite)
            .ThenByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        return items.Select(s => s.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CodeSnippetDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var items = await DetailQuery()
            .Where(s => s.Title.Contains(term) || s.Code.Contains(term) || (s.Description != null && s.Description.Contains(term)))
            .OrderByDescending(s => s.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
        return items.Select(s => s.ToDto()).ToList();
    }

    private IQueryable<CodeSnippet> DetailQuery() =>
        db.CodeSnippets.AsNoTracking().Include(s => s.Project);

    private async Task<CodeSnippetDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var snippet = await DetailQuery().FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Snippet {id} was not found.");
        return snippet.ToDto();
    }
}
