using System.Globalization;

namespace SongMetainfoBrowser.App;

internal static class DateTimeDisplay
{
    public static string Format(DateTime value)
    {
        return value.ToString("g", CultureInfo.CurrentCulture);
    }
}
