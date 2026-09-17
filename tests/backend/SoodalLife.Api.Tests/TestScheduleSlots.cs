namespace SoodalLife.Api.Tests;

internal static class TestScheduleSlots
{
    public static DateTimeOffset Future(int days = 2)
    {
        var source = DateTimeOffset.UtcNow.AddDays(days);
        var minute = source.Minute < 30 ? 30 : 0;
        var hour = source.Minute < 30 ? source.Hour : (source.Hour + 1) % 24;
        var date = source.Minute < 30 || source.Hour < 23 ? source.Date : source.Date.AddDays(1);
        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, TimeSpan.Zero);
    }

    public static DateTime FutureDateTime(int days = 2) => Future(days).UtcDateTime;
}
