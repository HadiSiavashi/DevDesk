using DevDesk.Application.Parsing;
using DevDesk.Domain.Enums;
using FluentAssertions;

namespace DevDesk.Tests;

public class QuickAddParserTests
{
    private static readonly DateOnly Today = new(2026, 8, 9); // Sunday

    [Fact]
    public void Parses_title_only()
    {
        var result = QuickAddParser.Parse("Fix payment API", Today);

        result.Title.Should().Be("Fix payment API");
        result.ProjectName.Should().BeNull();
        result.Priority.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.EstimatedMinutes.Should().BeNull();
    }

    [Fact]
    public void Parses_project_hashtag()
    {
        var result = QuickAddParser.Parse("Fix payment API #CRM", Today);

        result.Title.Should().Be("Fix payment API");
        result.ProjectName.Should().Be("CRM");
    }

    [Fact]
    public void Parses_tomorrow()
    {
        var result = QuickAddParser.Parse("Fix payment API tomorrow", Today);

        result.Title.Should().Be("Fix payment API");
        result.DueDate.Should().Be(Today.AddDays(1));
    }

    [Fact]
    public void Parses_high_priority()
    {
        var result = QuickAddParser.Parse("Fix payment API !high", Today);

        result.Title.Should().Be("Fix payment API");
        result.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void Parses_combined_project_tomorrow_and_duration()
    {
        var result = QuickAddParser.Parse("Fix payment API #CRM tomorrow 2h", Today);

        result.Title.Should().Be("Fix payment API");
        result.ProjectName.Should().Be("CRM");
        result.DueDate.Should().Be(Today.AddDays(1));
        result.EstimatedMinutes.Should().Be(120);
    }

    [Fact]
    public void Parses_today()
    {
        var result = QuickAddParser.Parse("Fix payment API today", Today);

        result.DueDate.Should().Be(Today);
        result.Title.Should().Be("Fix payment API");
    }

    [Fact]
    public void Parses_critical_priority()
    {
        var result = QuickAddParser.Parse("Fix payment API !critical", Today);

        result.Priority.Should().Be(TaskPriority.Critical);
        result.Title.Should().Be("Fix payment API");
    }

    [Fact]
    public void Parses_30m_duration()
    {
        var result = QuickAddParser.Parse("Fix payment API 30m", Today);

        result.EstimatedMinutes.Should().Be(30);
        result.Title.Should().Be("Fix payment API");
    }

    [Fact]
    public void Parses_monday_as_next_weekday()
    {
        // Today is Sunday 2026-08-09 → next Monday is 2026-08-10
        var result = QuickAddParser.Parse("Fix payment API monday", Today);

        result.DueDate.Should().Be(new DateOnly(2026, 8, 10));
        result.Title.Should().Be("Fix payment API");
    }

    [Fact]
    public void Parses_iso_date()
    {
        var result = QuickAddParser.Parse("Ship release 2026-09-01", Today);

        result.DueDate.Should().Be(new DateOnly(2026, 9, 1));
        result.Title.Should().Be("Ship release");
    }

    [Fact]
    public void Empty_input_throws()
    {
        var act = () => QuickAddParser.Parse("   ", Today);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tokens_only_without_title_throws()
    {
        var act = () => QuickAddParser.Parse("#CRM !high tomorrow", Today);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parses_persian_keywords_and_priorities()
    {
        // "اصلاح باگ احراز هویت #بک‌اند !فوری فردا ۴۵دقیقه"
        var result = QuickAddParser.Parse("اصلاح باگ احراز هویت #Auth !فوری فردا ۴۵دقیقه", Today);

        result.Title.Should().Be("اصلاح باگ احراز هویت");
        result.ProjectName.Should().Be("Auth");
        result.Priority.Should().Be(TaskPriority.Critical);
        result.DueDate.Should().Be(Today.AddDays(1));
        result.EstimatedMinutes.Should().Be(45);
    }

    [Fact]
    public void Parses_persian_weekdays_and_durations()
    {
        // Today is Sunday 2026-08-09 -> next دوشنبه is 2026-08-10
        var result = QuickAddParser.Parse("بررسی کدها !مهم دوشنبه ۲ساعت", Today);

        result.Title.Should().Be("بررسی کدها");
        result.Priority.Should().Be(TaskPriority.High);
        result.DueDate.Should().Be(new DateOnly(2026, 8, 10));
        result.EstimatedMinutes.Should().Be(120);
    }

    [Fact]
    public void Parses_jalali_date()
    {
        // 1405-05-18 is 2026-08-09
        var result = QuickAddParser.Parse("انتشار نسخه جدید 1405-05-18", Today);

        result.Title.Should().Be("انتشار نسخه جدید");
        result.DueDate.Should().Be(new DateOnly(2026, 8, 9));
    }
}
