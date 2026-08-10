using DevDesk.Application.Dtos;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;

namespace DevDesk.Tests;

public class DailyPlanTests
{
    [Fact]
    public void HasAllGoals_true_only_when_three_non_empty_goals()
    {
        var plan = new DailyPlan
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 8, 9),
            TopGoal1 = "Ship API",
            TopGoal2 = "Review PRs",
            TopGoal3 = "Write tests"
        };

        plan.HasAllGoals.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "B", "C")]
    [InlineData("A", null, "C")]
    [InlineData("A", "B", null)]
    [InlineData("A", "B", "  ")]
    [InlineData("", "B", "C")]
    public void HasAllGoals_false_when_any_goal_missing(string? g1, string? g2, string? g3)
    {
        var plan = new DailyPlan
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 8, 9),
            TopGoal1 = g1,
            TopGoal2 = g2,
            TopGoal3 = g3
        };

        plan.HasAllGoals.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_stores_three_goals_and_HasAllGoals()
    {
        var (db, clock, service) = TestDbFactory.CreateDailyPlanService();
        await using var _ = db;

        var dto = await service.UpdateAsync(clock.Today, new UpdateDailyPlanRequest
        {
            TopGoal1 = "Goal one",
            TopGoal2 = "Goal two",
            TopGoal3 = "Goal three",
            AvailableWorkMinutes = 480
        });

        dto.TopGoal1.Should().Be("Goal one");
        dto.TopGoal2.Should().Be("Goal two");
        dto.TopGoal3.Should().Be("Goal three");
        dto.HasAllGoals.Should().BeTrue();
    }

    [Fact]
    public async Task Workload_exceeds_available_sets_warning_flag()
    {
        var (db, clock, service) = TestDbFactory.CreateDailyPlanService();
        await using var _ = db;

        var today = clock.Today;
        db.Tasks.AddRange(
            new WorkTask
            {
                Id = Guid.NewGuid(),
                Title = "Big task",
                Status = WorkTaskStatus.Todo,
                DueDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                EstimatedMinutes = 300,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            },
            new WorkTask
            {
                Id = Guid.NewGuid(),
                Title = "Another",
                Status = WorkTaskStatus.InProgress,
                DueDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                EstimatedMinutes = 240,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            });
        await db.SaveChangesAsync();

        var dto = await service.UpdateAsync(today, new UpdateDailyPlanRequest
        {
            TopGoal1 = "A",
            TopGoal2 = "B",
            TopGoal3 = "C",
            AvailableWorkMinutes = 480
        });

        dto.EstimatedWorkloadMinutes.Should().Be(540);
        dto.AvailableWorkMinutes.Should().Be(480);
        dto.WorkloadExceedsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Workload_within_available_does_not_warn()
    {
        var (db, clock, service) = TestDbFactory.CreateDailyPlanService();
        await using var _ = db;

        var today = clock.Today;
        db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "Light",
            Status = WorkTaskStatus.Todo,
            DueDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EstimatedMinutes = 60,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var dto = await service.GetOrCreateAsync(today);

        dto.EstimatedWorkloadMinutes.Should().Be(60);
        dto.WorkloadExceedsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Estimate_defaults_missing_estimate_to_30_minutes()
    {
        var (db, clock, service) = TestDbFactory.CreateDailyPlanService();
        await using var _ = db;

        var today = clock.Today;
        db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "No estimate",
            Status = WorkTaskStatus.Todo,
            DueDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EstimatedMinutes = null,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var dto = await service.GetOrCreateAsync(today);

        dto.EstimatedWorkloadMinutes.Should().Be(30);
    }
}
