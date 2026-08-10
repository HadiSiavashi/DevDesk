using DevDesk.Application.Events;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms;

public static class ViewRegistration
{
    public static void RegisterViews(this NavigationService nav, IServiceScopeFactory scopeFactory, IServiceProvider services)
    {
        var events = services.GetRequiredService<IAppEventBus>();

        nav.Register("dashboard", _ => new DashboardView(scopeFactory, nav, events));
        nav.Register("myday", _ => new MyDayView(scopeFactory, nav, events));
        nav.Register("tasks", _ => new TasksView(scopeFactory, nav, events));
        nav.Register("task-detail", p => new TaskDetailView(scopeFactory, nav, p, events));
        nav.Register("projects", _ => new ProjectsView(scopeFactory, nav));
        nav.Register("project-detail", p => new ProjectDetailView(scopeFactory, nav, p));
        nav.Register("calendar", _ => new CalendarView(scopeFactory, nav));
        nav.Register("focus", _ => new FocusView(scopeFactory, nav, events));
        nav.Register("notes", _ => new NotesView(scopeFactory, nav));
        nav.Register("note-editor", p => new NoteEditorView(scopeFactory, nav, p));
        nav.Register("goals", _ => new GoalsView(scopeFactory, nav));
        nav.Register("habits", _ => new HabitsView(scopeFactory, nav));
        nav.Register("snippets", _ => new SnippetsView(scopeFactory, nav));
        nav.Register("snippet-editor", p => new SnippetEditorView(scopeFactory, nav, p));
        nav.Register("bookmarks", _ => new BookmarksView(scopeFactory, nav));
        nav.Register("environments", _ => new EnvironmentsView(scopeFactory, nav));
        nav.Register("knowledge", _ => new KnowledgeBaseView(scopeFactory, nav));
        nav.Register("analytics", _ => new AnalyticsView(scopeFactory, nav));
        nav.Register("productivity", _ => new AnalyticsView(scopeFactory, nav));
        nav.Register("reports", _ => new AnalyticsView(scopeFactory, nav));
        nav.Register("settings", _ => new SettingsView(scopeFactory, nav));
        nav.Register("dailyplan", _ => new DailyPlanningView(scopeFactory, nav));
        nav.Register("dailyreview", _ => new DailyReviewView(scopeFactory, nav));
    }
}
