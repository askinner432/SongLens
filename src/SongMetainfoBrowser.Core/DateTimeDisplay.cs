using System.Globalization;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Formats dates and times using the current regional settings.
/// </summary>
public static class DateTimeDisplay
{
    public static string Format(DateTime value)
    {
        return value.ToString("g", CultureInfo.CurrentCulture);
    }
}
