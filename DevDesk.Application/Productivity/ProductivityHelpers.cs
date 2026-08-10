using DevDesk.Application.Dtos;
using DevDesk.Domain.Services;

namespace DevDesk.Application.Productivity;

public static class ProductivityHelpers
{
    public static ProductivityScoreDto ToDto(ProductivityScoreResult result, DateOnly date) => new()
    {
        Total = result.Total,
        CompletionScore = result.CompletionScore,
        FocusScore = result.FocusScore,
        PlanningScore = result.PlanningScore,
        ReviewScore = result.ReviewScore,
        OverduePenalty = result.OverduePenalty,
        Explanation = result.Explanation,
        Date = date
    };

    public static ProductivityScoreDto Calculate(
        DateOnly date,
        int tasksCompletedToday,
        int tasksPlannedToday,
        int focusMinutesToday,
        int targetFocusMinutes,
        int overdueTaskCount,
        bool hasDailyPlan,
        bool hasDailyReview)
    {
        var result = ProductivityScoreCalculator.Calculate(
            tasksCompletedToday,
            tasksPlannedToday,
            focusMinutesToday,
            targetFocusMinutes,
            overdueTaskCount,
            hasDailyPlan,
            hasDailyReview);

        return ToDto(result, date);
    }
}
