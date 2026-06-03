using Microsoft.Win32;

namespace SongMetainfoBrowser.App;

internal enum SongHostApplication
{
    Unknown = 0,
    StudioOne7,
    FenderStudioPro8
}

internal sealed class SongLaunchPlan
{
    public required SongHostApplication Application { get; init; }
    public required string ApplicationName { get; init; }
    public required string? ExecutablePath { get; init; }
}

internal static class SongLaunchResolver
{
    public static SongLaunchPlan? CreatePlan(SongMetadata metadata)
    {
        var application = DeterminePrimaryApplication(metadata.Generator);
        if (application == SongHostApplication.Unknown)
        {
            return null;
        }

        return CreatePlan(application);
    }

    public static IReadOnlyList<SongHostApplication> GetLaunchTargets(SongMetadata metadata)
    {
        return GetLaunchTargets(metadata.Generator);
    }

    public static IReadOnlyList<SongHostApplication> GetLaunchTargets(string? generator)
    {
        if (string.IsNullOrWhiteSpace(generator))
        {
            return Array.Empty<SongHostApplication>();
        }

        if (generator.Contains("Studio Pro/8", StringComparison.OrdinalIgnoreCase))
        {
            return [SongHostApplication.FenderStudioPro8];
        }

        if (generator.Contains("Studio One/7", StringComparison.OrdinalIgnoreCase)
            || generator.Contains("Studio One/6", StringComparison.OrdinalIgnoreCase))
        {
            return [SongHostApplication.StudioOne7, SongHostApplication.FenderStudioPro8];
        }

        return Array.Empty<SongHostApplication>();
    }

    public static SongLaunchPlan CreatePlan(SongHostApplication application)
    {
        return new SongLaunchPlan
        {
            Application = application,
            ApplicationName = GetApplicationName(application),
            ExecutablePath = ResolveExecutablePath(application)
        };
    }

    public static string GetApplicationName(SongHostApplication application)
    {
        return application switch
        {
            SongHostApplication.StudioOne7 => "Studio One 7",
            SongHostApplication.FenderStudioPro8 => "Fender Studio Pro 8",
            _ => "Compatible app"
        };
    }

    public static string GetExecutableFileName(SongHostApplication application)
    {
        return application switch
        {
            SongHostApplication.StudioOne7 => "Studio One.exe",
            SongHostApplication.FenderStudioPro8 => "Studio Pro.exe",
            _ => "*.exe"
        };
    }

    public static void SaveExecutablePath(SongHostApplication application, string executablePath)
    {
        switch (application)
        {
            case SongHostApplication.StudioOne7:
                BrowserConfigStore.SaveStudioOne7Path(executablePath);
                break;
            case SongHostApplication.FenderStudioPro8:
                BrowserConfigStore.SaveFenderStudioPro8Path(executablePath);
                break;
        }
    }

    private static SongHostApplication DeterminePrimaryApplication(string? generator)
    {
        return GetLaunchTargets(generator).FirstOrDefault(SongHostApplication.Unknown);
    }

    private static string? ResolveExecutablePath(SongHostApplication application)
    {
        var configuredPath = application switch
        {
            SongHostApplication.StudioOne7 => BrowserConfigStore.LoadStudioOne7Path(),
            SongHostApplication.FenderStudioPro8 => BrowserConfigStore.LoadFenderStudioPro8Path(),
            _ => null
        };

        if (IsUsableExecutable(configuredPath))
        {
            return configuredPath;
        }

        return AutoDetectExecutablePath(application);
    }

    private static string? AutoDetectExecutablePath(SongHostApplication application)
    {
        var expectedDisplayName = application switch
        {
            SongHostApplication.StudioOne7 => "PreSonus Studio One 7",
            SongHostApplication.FenderStudioPro8 => "Fender Studio Pro 8",
            _ => ""
        };

        var expectedExeName = GetExecutableFileName(application);
        foreach (var registryPath in GetUninstallRegistryPaths())
        {
            using var uninstallKey = Registry.LocalMachine.OpenSubKey(registryPath);
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var productKey = uninstallKey.OpenSubKey(subKeyName);
                if (productKey is null)
                {
                    continue;
                }

                var displayName = productKey.GetValue("DisplayName") as string;
                if (!string.Equals(displayName, expectedDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var installLocation = productKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(installLocation))
                {
                    var candidate = Path.Combine(installLocation, expectedExeName);
                    if (IsUsableExecutable(candidate))
                    {
                        return candidate;
                    }
                }

                var displayIcon = productKey.GetValue("DisplayIcon") as string;
                var executableFromDisplayIcon = NormalizeExecutablePath(displayIcon);
                if (IsUsableExecutable(executableFromDisplayIcon))
                {
                    return executableFromDisplayIcon;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetUninstallRegistryPaths()
    {
        yield return @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
    }

    private static string? NormalizeExecutablePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        var trimmed = rawPath.Trim();
        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex >= 0)
        {
            trimmed = trimmed[..commaIndex];
        }

        trimmed = trimmed.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool IsUsableExecutable(string? executablePath)
    {
        return !string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath);
    }
}
