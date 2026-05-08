namespace SongMetainfoBrowser.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            WindowsShell.SetSongLensAppId();

            File.WriteAllText(
                AppPaths.StartupLogPath,
                $"Starting SongLens at {DateTime.Now:O}{Environment.NewLine}");

            ApplicationConfiguration.Initialize();
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
