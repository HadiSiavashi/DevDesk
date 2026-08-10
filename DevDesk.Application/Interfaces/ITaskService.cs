using DevDesk.Application.Dtos;
using DevDesk.Domain.Enums;

namespace DevDesk.Application.Interfaces;

public interface ITaskService
{
    Task<WorkTaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<WorkTaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkTaskDto> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkTaskDto> ReopenAsync(Guid id, CancellationToken ct = default);
    Task<WorkTaskDto> ChangeStatusAsync(Guid id, WorkTaskStatus status, CancellationToken ct = default);
    Task<WorkTaskDto> ChangePriorityAsync(Guid id, TaskPriority priority, CancellationToken ct = default);
    Task<WorkTaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetTodayAsync(CancellationToken ct = default);
    /// <summary>Today board: overdue, due today, in-progress, and completed today — sorted for Focus/My Day.</summary>
    Task<IReadOnlyList<TaskListItemDto>> GetMyDayTasksAsync(Guid? activeFocusTaskId = null, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetUpcomingAsync(int days = 7, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetOverdueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetStarredAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetCompletedAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetAllAsync(int take = 500, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskListItemDto>> SearchAsync(string query, CancellationToken ct = default);
    Task<WorkTaskDto> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task<WorkTaskDto> CreateFromQuickAddAsync(string input, CancellationToken ct = default);
    Task<ChecklistItemDto> AddChecklistItemAsync(Guid taskId, CreateChecklistItemRequest request, CancellationToken ct = default);
    Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid taskId, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default);
    Task DeleteChecklistItemAsync(Guid taskId, Guid itemId, CancellationToken ct = default);
    Task<ChecklistItemDto> ToggleChecklistItemAsync(Guid taskId, Guid itemId, CancellationToken ct = default);
}
