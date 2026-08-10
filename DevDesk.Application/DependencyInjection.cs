using DevDesk.Application.Abstractions;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAppEventBus, AppEventBus>();

        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IFocusService, FocusService>();
        services.AddScoped<IDailyPlanService, DailyPlanService>();
        services.AddScoped<IDailyReviewService, DailyReviewService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IEnvironmentService, EnvironmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        // Concrete type so Infrastructure can decorate with WindowsNotificationService.
        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());

        return services;
    }
}
