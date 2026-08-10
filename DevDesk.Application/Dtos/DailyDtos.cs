namespace DevDesk.Application.Dtos;

public sealed class DailyPlanDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? TopGoal1 { get; set; }
    public string? TopGoal2 { get; set; }
    public string? TopGoal3 { get; set; }
    public string? Notes { get; set; }
    public int AvailableWorkMinutes { get; set; }
    public int EstimatedWorkloadMinutes { get; set; }
    public bool WorkloadExceedsAvailable { get; set; }
    public bool HasAllGoals { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdateDailyPlanRequest
{
    public string? TopGoal1 { get; set; }
    public string? TopGoal2 { get; set; }
    public string? TopGoal3 { get; set; }
    public string? Notes { get; set; }
    public int AvailableWorkMinutes { get; set; } = 480;
}

public sealed class DailyReviewDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? WhatWentWell { get; set; }
    public string? WhatDidNotGoWell { get; set; }
    public string? LessonsLearned { get; set; }
    public string? TomorrowPlan { get; set; }
    public int FocusMinutes { get; set; }
    public int CompletedTaskCount { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdateDailyReviewRequest
{
    public string? WhatWentWell { get; set; }
    public string? WhatDidNotGoWell { get; set; }
    public string? LessonsLearned { get; set; }
    public string? TomorrowPlan { get; set; }
}
