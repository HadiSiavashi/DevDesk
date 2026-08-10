namespace DevDesk.Domain.Entities;

public class DailyReview
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? WhatWentWell { get; set; }
    public string? WhatDidNotGoWell { get; set; }
    public string? LessonsLearned { get; set; }
    public string? TomorrowPlan { get; set; }
    public int FocusMinutes { get; set; }
    public int CompletedTaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(WhatWentWell) ||
        !string.IsNullOrWhiteSpace(WhatDidNotGoWell) ||
        !string.IsNullOrWhiteSpace(LessonsLearned) ||
        !string.IsNullOrWhiteSpace(TomorrowPlan);
}
