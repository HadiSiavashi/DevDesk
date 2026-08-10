using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class SearchService(IDevDeskDbContext db) : ISearchService
{
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, int take = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var limit = Math.Clamp(take, 1, 200);
        var results = new List<SearchResultDto>();

        var tasks = await db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)))
            .OrderByDescending(t => t.UpdatedAt)
            .Take(limit)
            .Select(t => new SearchResultDto
            {
                EntityType = "Task",
                Id = t.Id,
                Title = t.Title,
                Subtitle = t.Status.ToString(),
                ProjectName = t.Project != null ? t.Project.Name : null
            })
            .ToListAsync(ct);
        results.AddRange(tasks);

        var projects = await db.Projects.AsNoTracking()
            .Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)))
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new SearchResultDto
            {
                EntityType = "Project",
                Id = p.Id,
                Title = p.Name,
                Subtitle = p.IsArchived ? "Archived" : "Active"
            })
            .ToListAsync(ct);
        results.AddRange(projects);

        var notes = await db.Notes.AsNoTracking()
            .Include(n => n.Project)
            .Where(n => n.Title.Contains(term) || n.Content.Contains(term))
            .OrderByDescending(n => n.UpdatedAt)
            .Take(limit)
            .Select(n => new SearchResultDto
            {
                EntityType = "Note",
                Id = n.Id,
                Title = n.Title,
                Subtitle = n.IsKnowledgeBase ? "Knowledge" : "Note",
                ProjectName = n.Project != null ? n.Project.Name : null
            })
            .ToListAsync(ct);
        results.AddRange(notes);

        var bookmarks = await db.Bookmarks.AsNoTracking()
            .Include(b => b.Project)
            .Where(b => b.Title.Contains(term) || b.Url.Contains(term))
            .OrderBy(b => b.Title)
            .Take(limit)
            .Select(b => new SearchResultDto
            {
                EntityType = "Bookmark",
                Id = b.Id,
                Title = b.Title,
                Subtitle = b.Url,
                ProjectName = b.Project != null ? b.Project.Name : null
            })
            .ToListAsync(ct);
        results.AddRange(bookmarks);

        var snippets = await db.CodeSnippets.AsNoTracking()
            .Include(s => s.Project)
            .Where(s => s.Title.Contains(term) || s.Code.Contains(term))
            .OrderByDescending(s => s.UpdatedAt)
            .Take(limit)
            .Select(s => new SearchResultDto
            {
                EntityType = "Snippet",
                Id = s.Id,
                Title = s.Title,
                Subtitle = s.Language,
                ProjectName = s.Project != null ? s.Project.Name : null
            })
            .ToListAsync(ct);
        results.AddRange(snippets);

        var goals = await db.Goals.AsNoTracking()
            .Where(g => g.Title.Contains(term) || (g.Description != null && g.Description.Contains(term)))
            .OrderByDescending(g => g.UpdatedAt)
            .Take(limit)
            .Select(g => new SearchResultDto
            {
                EntityType = "Goal",
                Id = g.Id,
                Title = g.Title,
                Subtitle = $"{g.Progress}%"
            })
            .ToListAsync(ct);
        results.AddRange(goals);

        return results.Take(limit).ToList();
    }
}
