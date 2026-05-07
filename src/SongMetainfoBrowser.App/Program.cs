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
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "startup.log"),
                $"Starting SongLens at {DateTime.Now:O}{Environment.NewLine}");

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
            File.WriteAllText(logPath, ex.ToString());
            MessageBox.Show(
                $"SongLens could not start.\n\n{ex.Message}\n\nDetails were written to:\n{logPath}",
                "SongLens",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }    
}
