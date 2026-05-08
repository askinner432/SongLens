using System.Runtime.InteropServices;

namespace SongMetainfoBrowser.App;

internal static class WindowsShell
{
    private const string AppUserModelId = "askin.SongLens";

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

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
}
