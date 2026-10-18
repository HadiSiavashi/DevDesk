using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevDesk.Tests;

public class SqliteDatabaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DevDeskDbContext _context;

    public SqliteDatabaseTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DevDeskDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DevDeskDbContext(options);
    }

    [Fact]
    public async Task Sqlite_EnsureCreated_And_Seed_Succeeds()
    {
        var initializer = new DatabaseInitializer(_context, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync(autoMigrate: true, seedDemoData: true);

        var users = await _context.Users.ToListAsync();
        users.Should().NotBeEmpty();

        var projects = await _context.Projects.ToListAsync();
        projects.Should().NotBeEmpty();

        var tasks = await _context.Tasks.ToListAsync();
        tasks.Should().NotBeEmpty();

        // Verify creating a new task and querying it
        var newTask = new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "SQLite Task Test",
            Status = WorkTaskStatus.Todo,
            Priority = TaskPriority.High,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Tasks.FindAsync(newTask.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("SQLite Task Test");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
