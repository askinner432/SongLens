using System.Diagnostics;

namespace SongMetainfoBrowser.App;

internal static class MixdownAudioPlayback
{
    private static readonly string[] SupportedExtensions =
    [
        ".mp3",
        ".wav",
        ".wave",
        ".flac",
        ".aiff",
        ".aif",
        ".m4a",
        ".aac",
        ".ogg",
        ".wma"
    ];

    private static readonly HashSet<string> SupportedExtensionSet =
        SupportedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> FindMatchingFiles(SongMetadata metadata)
    {
        var mixdownFolder = Path.Combine(metadata.Folder, "Mixdown");
        if (!Directory.Exists(mixdownFolder))
        {
            return Array.Empty<string>();
        }

        var songName = Path.GetFileNameWithoutExtension(metadata.FileName);
        try
        {
            return Directory.EnumerateFiles(mixdownFolder, "*", SearchOption.TopDirectoryOnly)
                .Where(path => SupportedExtensionSet.Contains(Path.GetExtension(path)))
                .Select(path => new
                {
                    Path = path,
                    Version = GetMixdownVersion(Path.GetFileNameWithoutExtension(path), songName)
                })
                .Where(item => item.Version is not null)
                .OrderBy(item => GetExtensionOrder(Path.GetExtension(item.Path)))
                .ThenByDescending(item => item.Version)
                .ThenBy(item => item.Path, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => item.Path)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    public static bool HasMatchingFile(SongMetadata metadata)
    {
        return FindMatchingFiles(metadata).Count > 0;
    }

    public static string GetFormatLabel(string audioPath)
    {
        return Path.GetExtension(audioPath).TrimStart('.').ToUpperInvariant();
    }

    public static void Play(string audioPath)
    {
        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("The mixdown audio file was not found.", audioPath);
        }

        var useCustomPlayer = BrowserConfigStore.LoadUseCustomAudioPlayer();
        var customPlayerPath = BrowserConfigStore.LoadCustomAudioPlayerPath();
        if (useCustomPlayer)
        {
            if (string.IsNullOrWhiteSpace(customPlayerPath) || !File.Exists(customPlayerPath))
            {
                throw new FileNotFoundException(
                    "The custom audio player could not be found. Choose another player in Preferences.",
                    customPlayerPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = customPlayerPath,
                WorkingDirectory = Path.GetDirectoryName(customPlayerPath),
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(audioPath);
            Process.Start(startInfo);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = audioPath,
            WorkingDirectory = Path.GetDirectoryName(audioPath),
            UseShellExecute = true
        });
    }

    private static int GetExtensionOrder(string extension)
    {
        for (var index = 0; index < SupportedExtensions.Length; index++)
        {
            if (string.Equals(extension, SupportedExtensions[index], StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static int? GetMixdownVersion(string fileNameWithoutExtension, string songName)
    {
        if (string.Equals(fileNameWithoutExtension, songName, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (!fileNameWithoutExtension.StartsWith(songName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var versionSuffix = fileNameWithoutExtension[songName.Length..].TrimStart();
        if (versionSuffix.Length < 3
            || !versionSuffix.StartsWith('(')
            || !versionSuffix.EndsWith(')'))
        {
            return null;
        }

        var versionText = versionSuffix[1..^1];
        return int.TryParse(versionText, out var version) && version >= 2
            ? version
            : null;
    }
}
