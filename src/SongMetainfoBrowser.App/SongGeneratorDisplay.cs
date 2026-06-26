namespace SongMetainfoBrowser.App;

internal static class SongGeneratorDisplay
{
    public static string? ToFriendlyDisplay(string? generator)
    {
        if (!TryParse(generator, out var productName, out var versionText))
        {
            return string.IsNullOrWhiteSpace(generator) ? null : generator;
        }

        return string.IsNullOrWhiteSpace(versionText)
            ? productName
            : $"{productName} {versionText}";
    }

    public static string GetLaunchActionText(string? generator)
    {
        return TryParse(generator, out var productName, out var versionText)
            ? $"Open in {productName} {ExtractMajorVersion(versionText)}"
            : "Open in Compatible App";
    }

    public static string GetLaunchStatusText(string? generator)
    {
        return TryParse(generator, out var productName, out var versionText)
            ? string.IsNullOrWhiteSpace(versionText) ? productName : $"{productName} {ExtractMajorVersion(versionText)}"
            : "compatible app";
    }

    public static string? GetSearchQualifier(string? generator)
    {
        if (!TryParse(generator, out var productName, out var versionText))
        {
            return string.IsNullOrWhiteSpace(generator) ? null : generator;
        }

        var majorVersion = ExtractMajorVersion(versionText);
        if (string.IsNullOrWhiteSpace(majorVersion))
        {
            return productName;
        }

        return $"{productName} {majorVersion}.x";
    }

    private static bool TryParse(string? generator, out string productName, out string versionText)
    {
        productName = "";
        versionText = "";

        if (string.IsNullOrWhiteSpace(generator))
        {
            return false;
        }

        var trimmed = generator.Trim();
        var separatorIndex = trimmed.IndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
        {
            return false;
        }

        var productToken = trimmed[..separatorIndex].Trim();
        versionText = trimmed[(separatorIndex + 1)..].Trim();

        productName = productToken switch
        {
            "Studio One" => "Studio One",
            "Studio Pro" => "Fender Studio Pro",
            _ => productToken
        };

        return !string.IsNullOrWhiteSpace(productName);
    }

    private static string ExtractMajorVersion(string? versionText)
    {
        if (string.IsNullOrWhiteSpace(versionText))
        {
            return "";
        }

        var dotIndex = versionText.IndexOf('.');
        return dotIndex > 0 ? versionText[..dotIndex] : versionText;
    }
}
