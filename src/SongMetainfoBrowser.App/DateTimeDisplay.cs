using System.Globalization;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Formats dates and times using the current Windows regional settings.
/// </summary>
internal static class DateTimeDisplay
{
    public static string Format(DateTime value)
    {
        return value.ToString("g", CultureInfo.CurrentCulture);
    }
}
