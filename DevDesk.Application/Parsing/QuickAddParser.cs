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
    private static readonly Regex JalaliDateRegex = JalaliDatePattern();
    private static readonly PersianCalendar PersianCal = new();

    private static readonly HashSet<string> Weekdays = new(StringComparer.OrdinalIgnoreCase)
    {
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
        "mon", "tue", "wed", "thu", "fri", "sat", "sun",
        "شنبه", "یکشنبه", "یک‌شنبه", "دوشنبه", "دو‌شنبه", "سه‌شنبه", "چهارشنبه", "چهار‌شنبه", "پنجشنبه", "پنج‌شنبه", "جمعه"
    };

    public static QuickAddResult Parse(string input, DateOnly? today = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.", nameof(input));

        var reference = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var normalized = NormalizeDigits(input.Trim());
        var tokens = Tokenize(normalized);
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

    private static string NormalizeDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (ch is >= '۰' and <= '۹')
                chars[i] = (char)('0' + (ch - '۰'));
            else if (ch is >= '٠' and <= '٩')
                chars[i] = (char)('0' + (ch - '٠'));
        }
        return new string(chars);
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
        var clean = value.Trim().ToLowerInvariant().Replace("\u200c", "");
        switch (clean)
        {
            case "low" or "l" or "پایین" or "کم":
                priority = TaskPriority.Low;
                return true;
            case "medium" or "med" or "m" or "متوسط" or "معمولی" or "عادی":
                priority = TaskPriority.Medium;
                return true;
            case "high" or "h" or "بالا" or "مهم" or "زیاد":
                priority = TaskPriority.High;
                return true;
            case "critical" or "crit" or "c" or "urgent" or "فوری" or "بحرانی" or "اضطراری" or "حیاتی":
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
        var lower = token.ToLowerInvariant().Replace("\u200c", "");

        if (lower is "today" or "امروز")
        {
            dueDate = today;
            return true;
        }

        if (lower is "tomorrow" or "فردا")
        {
            dueDate = today.AddDays(1);
            return true;
        }

        if (lower is "پسفردا" or "پس‌فردا")
        {
            dueDate = today.AddDays(2);
            return true;
        }

        if (Weekdays.Contains(token) || Weekdays.Contains(lower))
        {
            dueDate = NextWeekday(today, NormalizeWeekday(lower));
            return true;
        }

        var jalaliMatch = JalaliDateRegex.Match(token);
        if (jalaliMatch.Success)
        {
            var year = int.Parse(jalaliMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(jalaliMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(jalaliMatch.Groups[3].Value, CultureInfo.InvariantCulture);
            if (year is >= 1300 and <= 1500 && month is >= 1 and <= 12 && day is >= 1 and <= 31)
            {
                try
                {
                    var dt = PersianCal.ToDateTime(year, month, day, 0, 0, 0, 0);
                    dueDate = DateOnly.FromDateTime(dt);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        if (IsoDateRegex.IsMatch(token) &&
            DateOnly.TryParseExact(token, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso) &&
            iso.Year >= 1900)
        {
            dueDate = iso;
            return true;
        }

        return false;
    }

    private static DayOfWeek NormalizeWeekday(string value) => value switch
    {
        "mon" or "monday" or "دوشنبه" or "دو‌شنبه" => DayOfWeek.Monday,
        "tue" or "tuesday" or "سه‌شنبه" or "سه شنبه" => DayOfWeek.Tuesday,
        "wed" or "wednesday" or "چهارشنبه" or "چهار‌شنبه" => DayOfWeek.Wednesday,
        "thu" or "thursday" or "پنجشنبه" or "پنج‌شنبه" => DayOfWeek.Thursday,
        "fri" or "friday" or "جمعه" => DayOfWeek.Friday,
        "sat" or "saturday" or "شنبه" => DayOfWeek.Saturday,
        "sun" or "sunday" or "یکشنبه" or "یک‌شنبه" => DayOfWeek.Sunday,
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
            "m" or "min" or "mins" or "minute" or "minutes" or "دقیقه" => amount,
            "h" or "hr" or "hrs" or "hour" or "hours" or "ساعت" => amount * 60,
            _ => 0
        };
        return minutes > 0;
    }

    [GeneratedRegex(@"^(\d+)(m|min|mins|minute|minutes|h|hr|hrs|hour|hours|دقیقه|ساعت)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDatePattern();

    [GeneratedRegex(@"^(\d{4})[/-](\d{1,2})[/-](\d{1,2})$", RegexOptions.CultureInvariant)]
    private static partial Regex JalaliDatePattern();
}
