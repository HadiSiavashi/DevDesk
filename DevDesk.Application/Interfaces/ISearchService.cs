using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, int take = 50, CancellationToken ct = default);
}
