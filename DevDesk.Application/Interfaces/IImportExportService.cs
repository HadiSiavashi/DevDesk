using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface IImportExportService
{
    Task<ExportDataDto> ExportAsync(CancellationToken ct = default);
    Task<string> ExportJsonAsync(CancellationToken ct = default);
    Task<ImportResultDto> ImportJsonAsync(string json, bool merge = true, CancellationToken ct = default);
}
