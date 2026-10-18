using System.Globalization;

namespace DevDesk.WinForms.Localization;

public static class DateFormatter
{
    private static readonly string[] PersianMonths =
        ["", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];

    private static readonly string[] PersianDayNames =
        ["یک‌شنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه"];

    private static readonly string[] PersianAbbrevDayNames =
        ["یک", "دو", "سه", "چهار", "پنج", "جمعه", "شنبه"];

    public static string FormatLongDate(DateTime dt)
    {
        if (LocalizationService.Instance.IsRtl)
        {
            var pc = new PersianCalendar();
            var pYear = pc.GetYear(dt);
            var pMonth = pc.GetMonth(dt);
            var pDay = pc.GetDayOfMonth(dt);
            var dow = (int)dt.DayOfWeek;
            var dayName = PersianDayNames[dow];
            return $"{dayName}، {pDay} {PersianMonths[pMonth]} {pYear}";
        }

        return dt.ToString("dddd, MMMM d, yyyy", LocalizationService.Instance.CurrentCulture);
    }

    public static string FormatMonthYear(DateTime dt)
    {
        if (LocalizationService.Instance.IsRtl)
        {
            var pc = new PersianCalendar();
            var pYear = pc.GetYear(dt);
            var pMonth = pc.GetMonth(dt);
            return $"{PersianMonths[pMonth]} {pYear}";
        }

        return dt.ToString("MMMM yyyy", LocalizationService.Instance.CurrentCulture);
    }

    public static string[] GetDayHeaderNames()
    {
        if (LocalizationService.Instance.IsRtl)
            return PersianAbbrevDayNames;

        return ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    }
}
