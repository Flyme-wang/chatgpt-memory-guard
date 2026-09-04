namespace ChatGPTMemoryGuard;

internal static class Program
{
    private const string MutexName = @"Local\ChatGPTMemoryGuard.Singleton";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ChatGPT Memory Guard 已经在运行。", "ChatGPT Memory Guard", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMemoryGuard");
        var log = new LogWriter(Path.Combine(dataDirectory, "logs"));

        try
        {
            var settingsStore = new SettingsStore(Path.Combine(dataDirectory, "settings.json"));
            SettingsLoadResult loadResult = settingsStore.Load();
            if (loadResult.Error is not null)
            {
                log.Write($"配置损坏，已恢复默认值：{loadResult.Error.Message}");
            }

            string executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("无法确定程序路径。");
            using var context = new TrayApplicationContext(
                loadResult.Settings,
                settingsStore,
                log,
                new StartupRegistration(executablePath));
            Application.Run(context);
        }
        catch (Exception error)
        {
            log.Write($"程序发生严重错误：{error}");
            MessageBox.Show($"程序无法继续运行：{error.Message}", "ChatGPT Memory Guard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
