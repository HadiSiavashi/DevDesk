namespace DevDesk.Domain.Services;

/// <summary>
/// Transparent productivity score calculation.
/// Score range: 0–100.
/// </summary>
public static class ProductivityScoreCalculator
{
    public const int MaxScore = 100;

    public static ProductivityScoreResult Calculate(
        int tasksCompletedToday,
        int tasksPlannedToday,
        int focusMinutesToday,
        int targetFocusMinutes,
        int overdueTaskCount,
        bool hasDailyPlan,
        bool hasDailyReview)
    {
        var completionScore = CalculateCompletionScore(tasksCompletedToday, tasksPlannedToday);
        var focusScore = CalculateFocusScore(focusMinutesToday, targetFocusMinutes);
        var overduePenalty = CalculateOverduePenalty(overdueTaskCount);
        var planningScore = hasDailyPlan ? 15 : 0;
        var reviewScore = hasDailyReview ? 10 : 0;

        var raw = completionScore + focusScore + planningScore + reviewScore - overduePenalty;
        var total = Math.Clamp(raw, 0, MaxScore);

        return new ProductivityScoreResult(
            Total: total,
            CompletionScore: completionScore,
            FocusScore: focusScore,
            PlanningScore: planningScore,
            ReviewScore: reviewScore,
            OverduePenalty: overduePenalty,
            Explanation:
                $"Completion {completionScore}/40 + Focus {focusScore}/35 + Plan {planningScore}/15 + Review {reviewScore}/10 − Overdue {overduePenalty} = {total}");
    }

    private static int CalculateCompletionScore(int completed, int planned)
    {
        if (planned <= 0)
            return completed > 0 ? 30 : 10;

        var ratio = Math.Min(1.0, (double)completed / planned);
        return (int)Math.Round(ratio * 40);
    }

    private static int CalculateFocusScore(int focusMinutes, int targetMinutes)
    {
        if (targetMinutes <= 0)
            targetMinutes = 120;

        var ratio = Math.Min(1.0, (double)focusMinutes / targetMinutes);
        return (int)Math.Round(ratio * 35);
    }

    private static int CalculateOverduePenalty(int overdueCount)
    {
        if (overdueCount <= 0)
            return 0;

        return Math.Min(25, overdueCount * 5);
    }
}

public sealed record ProductivityScoreResult(
    int Total,
    int CompletionScore,
    int FocusScore,
    int PlanningScore,
    int ReviewScore,
    int OverduePenalty,
    string Explanation);
