using System.Runtime.InteropServices;

namespace SongMetainfoBrowser.App;

internal static class WindowsShell
{
    private const string AppUserModelId = "askin.SongLens";
    private const uint EmSetCueBanner = 0x1501;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);

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
}
