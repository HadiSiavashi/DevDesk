using DevDesk.Domain.Services;
using FluentAssertions;

namespace DevDesk.Tests;

public class ProductivityScoreTests
{
    [Fact]
    public void Perfect_day_scores_100()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 4,
            tasksPlannedToday: 4,
            focusMinutesToday: 120,
            targetFocusMinutes: 120,
            overdueTaskCount: 0,
            hasDailyPlan: true,
            hasDailyReview: true);

        result.Total.Should().Be(100);
        result.CompletionScore.Should().Be(40);
        result.FocusScore.Should().Be(35);
        result.PlanningScore.Should().Be(15);
        result.ReviewScore.Should().Be(10);
        result.OverduePenalty.Should().Be(0);
    }

    [Fact]
    public void Zero_planned_with_no_completions_gives_baseline_completion_10()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 0,
            tasksPlannedToday: 0,
            focusMinutesToday: 0,
            targetFocusMinutes: 120,
            overdueTaskCount: 0,
            hasDailyPlan: false,
            hasDailyReview: false);

        result.CompletionScore.Should().Be(10);
        result.FocusScore.Should().Be(0);
        result.Total.Should().Be(10);
    }

    [Fact]
    public void Zero_planned_with_completions_gives_completion_30()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 2,
            tasksPlannedToday: 0,
            focusMinutesToday: 0,
            targetFocusMinutes: 120,
            overdueTaskCount: 0,
            hasDailyPlan: false,
            hasDailyReview: false);

        result.CompletionScore.Should().Be(30);
    }

    [Fact]
    public void Completion_ratio_is_capped_at_40()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 10,
            tasksPlannedToday: 2,
            focusMinutesToday: 0,
            targetFocusMinutes: 120,
            overdueTaskCount: 0,
            hasDailyPlan: false,
            hasDailyReview: false);

        result.CompletionScore.Should().Be(40);
    }

    [Fact]
    public void Focus_uses_default_target_120_when_target_non_positive()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 0,
            tasksPlannedToday: 1,
            focusMinutesToday: 60,
            targetFocusMinutes: 0,
            overdueTaskCount: 0,
            hasDailyPlan: false,
            hasDailyReview: false);

        // 60/120 * 35 = 17.5 → 18
        result.FocusScore.Should().Be(18);
    }

    [Fact]
    public void Focus_score_caps_at_35()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 0,
            tasksPlannedToday: 1,
            focusMinutesToday: 999,
            targetFocusMinutes: 120,
            overdueTaskCount: 0,
            hasDailyPlan: false,
            hasDailyReview: false);

        result.FocusScore.Should().Be(35);
    }

    [Fact]
    public void Overdue_penalty_is_5_per_task_capped_at_25()
    {
        var three = ProductivityScoreCalculator.Calculate(0, 1, 0, 120, 3, false, false);
        three.OverduePenalty.Should().Be(15);

        var six = ProductivityScoreCalculator.Calculate(0, 1, 0, 120, 6, false, false);
        six.OverduePenalty.Should().Be(25);

        var none = ProductivityScoreCalculator.Calculate(0, 1, 0, 120, 0, false, false);
        none.OverduePenalty.Should().Be(0);
    }

    [Fact]
    public void Total_clamps_to_zero_when_penalties_dominate()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 0,
            tasksPlannedToday: 10,
            focusMinutesToday: 0,
            targetFocusMinutes: 120,
            overdueTaskCount: 10,
            hasDailyPlan: false,
            hasDailyReview: false);

        result.CompletionScore.Should().Be(0);
        result.OverduePenalty.Should().Be(25);
        result.Total.Should().Be(0);
    }

    [Fact]
    public void Total_clamps_to_100()
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday: 100,
            tasksPlannedToday: 1,
            focusMinutesToday: 1000,
            targetFocusMinutes: 1,
            overdueTaskCount: 0,
            hasDailyPlan: true,
            hasDailyReview: true);

        result.Total.Should().Be(100);
    }

    [Fact]
    public void Partial_completion_rounds_ratio()
    {
        // 1/3 * 40 = 13.333 → 13
        var result = ProductivityScoreCalculator.Calculate(1, 3, 0, 120, 0, false, false);
        result.CompletionScore.Should().Be(13);
    }

    [Fact]
    public void Explanation_includes_component_breakdown()
    {
        var result = ProductivityScoreCalculator.Calculate(2, 4, 60, 120, 1, true, false);

        result.Explanation.Should().Contain("Completion");
        result.Explanation.Should().Contain("Focus");
        result.Explanation.Should().Contain("Overdue");
        result.Explanation.Should().Contain(result.Total.ToString());
    }
}
