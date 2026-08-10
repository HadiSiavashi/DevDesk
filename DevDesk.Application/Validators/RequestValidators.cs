using DevDesk.Application.Dtos;
using FluentValidation;

namespace DevDesk.Application.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0).When(x => x.EstimatedMinutes.HasValue);
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0).When(x => x.EstimatedMinutes.HasValue);
    }
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateDailyPlanRequestValidator : AbstractValidator<UpdateDailyPlanRequest>
{
    public UpdateDailyPlanRequestValidator()
    {
        RuleFor(x => x.TopGoal1).MaximumLength(500);
        RuleFor(x => x.TopGoal2).MaximumLength(500);
        RuleFor(x => x.TopGoal3).MaximumLength(500);
        RuleFor(x => x.AvailableWorkMinutes).InclusiveBetween(0, 24 * 60);
        RuleFor(x => x.Notes).MaximumLength(8000);
    }
}

public sealed class UpdateDailyReviewRequestValidator : AbstractValidator<UpdateDailyReviewRequest>
{
    public UpdateDailyReviewRequestValidator()
    {
        RuleFor(x => x.WhatWentWell).MaximumLength(4000);
        RuleFor(x => x.WhatDidNotGoWell).MaximumLength(4000);
        RuleFor(x => x.LessonsLearned).MaximumLength(4000);
        RuleFor(x => x.TomorrowPlan).MaximumLength(4000);
    }
}

public sealed class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty();
    }
}

public sealed class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
{
    public CreateHabitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateBookmarkRequestValidator : AbstractValidator<CreateBookmarkRequest>
{
    public CreateBookmarkRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateSnippetRequestValidator : AbstractValidator<CreateSnippetRequest>
{
    public CreateSnippetRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class CreateCalendarEventRequestValidator : AbstractValidator<CreateCalendarEventRequest>
{
    public CreateCalendarEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt);
    }
}

public sealed class CreateEnvironmentRequestValidator : AbstractValidator<CreateEnvironmentRequest>
{
    public CreateEnvironmentRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AppPreferencesDtoValidator : AbstractValidator<AppPreferencesDto>
{
    public AppPreferencesDtoValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetFocusMinutesPerDay).InclusiveBetween(0, 24 * 60);
        RuleFor(x => x.DefaultAvailableWorkMinutes).InclusiveBetween(0, 24 * 60);
        RuleFor(x => x.PomodoroWorkMinutes).InclusiveBetween(1, 180);
        RuleFor(x => x.PomodoroShortBreakMinutes).InclusiveBetween(1, 60);
        RuleFor(x => x.PomodoroLongBreakMinutes).InclusiveBetween(1, 120);
        RuleFor(x => x.SessionsUntilLongBreak).InclusiveBetween(1, 12);
    }
}
