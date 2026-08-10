using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface ICalendarService
{
    Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, CancellationToken ct = default);
    Task<CalendarEventDto> UpdateAsync(Guid id, UpdateCalendarEventRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CalendarEventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarEventDto>> GetRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
