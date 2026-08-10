using DevDesk.Application.Abstractions;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Application.Services;
using DevDesk.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevDesk.Tests.Helpers;

public static class TestDbFactory
{
    public static DevDeskDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DevDeskDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new DevDeskDbContext(options);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, ImportExportService Service) CreateImportExportService(
        string? databaseName = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        ISettingsService settings = new SettingsService(db);
        var service = new ImportExportService(db, clock, settings);
        return (db, clock, service);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, DailyPlanService Service) CreateDailyPlanService(
        string? databaseName = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        var service = new DailyPlanService(db, clock);
        return (db, clock, service);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, FocusService Service) CreateFocusService(
        string? databaseName = null,
        PomodoroOptions? pomodoro = null,
        FocusOptions? focus = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        var service = new FocusService(
            db,
            clock,
            Options.Create(pomodoro ?? new PomodoroOptions()),
            Options.Create(focus ?? new FocusOptions()),
            new AppEventBus());
        return (db, clock, service);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, TaskService Service, AppEventBus Events) CreateTaskService(
        string? databaseName = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        var events = new AppEventBus();
        var service = new TaskService(db, clock, events);
        return (db, clock, service, events);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, ProjectService Service) CreateProjectService(
        string? databaseName = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        var service = new ProjectService(db, clock);
        return (db, clock, service);
    }

    public static (DevDeskDbContext Db, FakeClock Clock, CalendarService Service) CreateCalendarService(
        string? databaseName = null)
    {
        var db = CreateDbContext(databaseName);
        var clock = new FakeClock(new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc));
        var service = new CalendarService(db);
        return (db, clock, service);
    }
}
