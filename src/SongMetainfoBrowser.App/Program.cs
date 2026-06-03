namespace SongMetainfoBrowser.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    ///  Startup is wrapped so installer or packaging issues can still surface a helpful log path.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            WindowsShell.SetSongLensAppId();
            DiagnosticLog.Reset();

            File.WriteAllText(
                AppPaths.StartupLogPath,
                $"Starting SongLens at {DateTime.Now:O}{Environment.NewLine}");

            ApplicationConfiguration.Initialize();
            if (BrowserConfigStore.GetConfigLoadWarning() is { } configLoadWarning)
            {
                MessageBox.Show(
                    configLoadWarning,
                    "SongLens Config Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            var logPath = AppPaths.StartupErrorLogPath;
            try
            {
                File.WriteAllText(logPath, ex.ToString());
            }
            catch
            {
                logPath = "Unavailable";
            }

            MessageBox.Show(
                $"SongLens could not start.\n\n{ex.Message}\n\nDetails were written to:\n{logPath}",
                "SongLens",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }    
}
