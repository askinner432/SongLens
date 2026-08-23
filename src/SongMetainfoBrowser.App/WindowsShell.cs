using System.Runtime.InteropServices;
using System.Text;

namespace SongMetainfoBrowser.App;

internal static class WindowsShell
{
    private const string AppUserModelId = "askin.SongLens";
    private const uint EmSetCueBanner = 0x1501;
    private const uint AssocStrFriendlyAppName = 4;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern uint AssocQueryString(
        uint flags,
        uint str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder pszOut,
        ref uint pcchOut);

    public static void SetSongLensAppId()
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch
        {
            // Ignore shell identity failures and continue startup.
        }
    }

    public static void SetCueBanner(TextBox textBox, string cueText)
    {
        void ApplyCueBanner()
        {
            try
            {
                SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, cueText);
            }
            catch
            {
                // Ignore cue banner failures and continue startup.
            }
        }

        if (textBox.IsHandleCreated)
        {
            ApplyCueBanner();
            return;
        }

        textBox.HandleCreated += (_, _) => ApplyCueBanner();
    }

    public static string? GetDefaultApplicationFriendlyName(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return null;
        }

        var normalizedExtension = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
        try
        {
            uint length = 512;
            var buffer = new StringBuilder((int)length);
            var result = AssocQueryString(
                0,
                AssocStrFriendlyAppName,
                normalizedExtension,
                null,
                buffer,
                ref length);
            var friendlyName = buffer.ToString().Trim();
            return result == 0 && !string.IsNullOrWhiteSpace(friendlyName)
                ? friendlyName
                : null;
        }
        catch
        {
            return null;
        }
    }
}
