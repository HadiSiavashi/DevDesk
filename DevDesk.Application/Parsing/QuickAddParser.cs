using System.Globalization;
using System.Text.RegularExpressions;
using DevDesk.Domain.Enums;

namespace DevDesk.Application.Parsing;

/// <summary>
/// Deterministic parser for quick-add task strings.
/// Supports: #project, !priority, relative/absolute due dates, duration (30m/2h).
/// </summary>
public static partial class QuickAddParser
{
    private static readonly Regex DurationRegex = DurationPattern();
    private static readonly Regex IsoDateRegex = IsoDatePattern();
    private static readonly HashSet<string> Weekdays = new(StringComparer.OrdinalIgnoreCase)
    {
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
        "mon", "tue", "wed", "thu", "fri", "sat", "sun"
    };

    public static QuickAddResult Parse(string input, DateOnly? today = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.", nameof(input));

        var reference = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var tokens = Tokenize(input.Trim());
        var titleParts = new List<string>();
        string? projectName = null;
        TaskPriority? priority = null;
        DateOnly? dueDate = null;
        int? estimatedMinutes = null;

        foreach (var token in tokens)
        {
            if (token.StartsWith('#') && token.Length > 1)
            {
                projectName = token[1..];
                continue;
            }

            if (token.StartsWith('!') && token.Length > 1 && TryParsePriority(token[1..], out var parsedPriority))
            {
                priority = parsedPriority;
                continue;
            }

            if (TryParseDueDate(token, reference, out var parsedDue))
            {
                dueDate = parsedDue;
                continue;
            }

            if (TryParseDuration(token, out var minutes))
            {
                estimatedMinutes = minutes;
                continue;
            }

            titleParts.Add(token);
        }

        var title = string.Join(' ', titleParts).Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new FormatException("Quick-add input must include a task title.");

        return new QuickAddResult
        {
            Title = title,
            ProjectName = projectName,
            Priority = priority,
            DueDate = dueDate,
            EstimatedMinutes = estimatedMinutes
        };
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Count > 0)
                {
                    tokens.Add(new string(current.ToArray()));
                    current.Clear();
                }

                continue;
            }

            current.Add(ch);
        }

        if (current.Count > 0)
            tokens.Add(new string(current.ToArray()));

        return tokens;
    }

    private static bool TryParsePriority(string value, out TaskPriority priority)
    {
        switch (value.ToLowerInvariant())
        {
            case "low":
            case "l":
                priority = TaskPriority.Low;
                return true;
            case "medium":
            case "med":
            case "m":
                priority = TaskPriority.Medium;
                return true;
            case "high":
            case "h":
                priority = TaskPriority.High;
                return true;
            case "critical":
            case "crit":
            case "c":
            case "urgent":
                priority = TaskPriority.Critical;
                return true;
            default:
                priority = default;
                return false;
        }
    }

    private static bool TryParseDueDate(string token, DateOnly today, out DateOnly dueDate)
    {
        dueDate = default;
        var lower = token.ToLowerInvariant();

        if (lower is "today")
        {
            dueDate = today;
            return true;
        }

        if (lower is "tomorrow")
        {
            dueDate = today.AddDays(1);
            return true;
        }

        if (Weekdays.Contains(lower))
        {
            dueDate = NextWeekday(today, NormalizeWeekday(lower));
            return true;
        }

        if (IsoDateRegex.IsMatch(token) &&
            DateOnly.TryParseExact(token, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            dueDate = iso;
            return true;
        }

        return false;
    }

    private static DayOfWeek NormalizeWeekday(string value) => value.ToLowerInvariant() switch
    {
        "mon" or "monday" => DayOfWeek.Monday,
        "tue" or "tuesday" => DayOfWeek.Tuesday,
        "wed" or "wednesday" => DayOfWeek.Wednesday,
        "thu" or "thursday" => DayOfWeek.Thursday,
        "fri" or "friday" => DayOfWeek.Friday,
        "sat" or "saturday" => DayOfWeek.Saturday,
        "sun" or "sunday" => DayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static DateOnly NextWeekday(DateOnly from, DayOfWeek target)
    {
        var daysAhead = ((int)target - (int)from.DayOfWeek + 7) % 7;
        if (daysAhead == 0)
            daysAhead = 7;
        return from.AddDays(daysAhead);
    }

    private static bool TryParseDuration(string token, out int minutes)
    {
        minutes = 0;
        var match = DurationRegex.Match(token);
        if (!match.Success)
            return false;

        var amount = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();
        minutes = unit switch
        {
            "m" or "min" or "mins" or "minute" or "minutes" => amount,
            "h" or "hr" or "hrs" or "hour" or "hours" => amount * 60,
            _ => 0
        };
        return minutes > 0;
    }

    [GeneratedRegex(@"^(\d+)(m|min|mins|minute|minutes|h|hr|hrs|hour|hours)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDatePattern();
}
