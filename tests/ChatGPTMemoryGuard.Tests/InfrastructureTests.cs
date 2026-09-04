using ChatGPTMemoryGuard;

namespace ChatGPTMemoryGuard.Tests;

[TestClass]
public sealed class InfrastructureTests
{
    private string _directory = null!;

    [TestInitialize]
    public void SetUp() =>
        _directory = Path.Combine(Path.GetTempPath(), "ChatGPTMemoryGuardTests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void CleanUp()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void StartupCommandQuotesExecutablePath()
    {
        string command = StartupRegistration.BuildCommand(@"C:\Program Files\ChatGPT Memory Guard\ChatGPTMemoryGuard.exe");

        Assert.AreEqual("\"C:\\Program Files\\ChatGPT Memory Guard\\ChatGPTMemoryGuard.exe\"", command);
    }

    [TestMethod]
    public void LogWriterCreatesDailyUtf8Log()
    {
        var now = new DateTimeOffset(2026, 9, 4, 14, 30, 0, TimeSpan.FromHours(8));
        var writer = new LogWriter(_directory, retentionDays: 14, clock: () => now);

        writer.Write("监测已启动");

        string logPath = Path.Combine(_directory, "guard-2026-09-04.log");
        Assert.IsTrue(File.Exists(logPath));
        StringAssert.Contains(File.ReadAllText(logPath), "监测已启动");
    }

    [TestMethod]
    public void LogWriterDeletesFilesOlderThanRetention()
    {
        Directory.CreateDirectory(_directory);
        string oldLog = Path.Combine(_directory, "guard-2026-08-01.log");
        File.WriteAllText(oldLog, "old");
        File.SetLastWriteTimeUtc(oldLog, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        var now = new DateTimeOffset(2026, 9, 4, 14, 30, 0, TimeSpan.Zero);

        _ = new LogWriter(_directory, retentionDays: 14, clock: () => now);

        Assert.IsFalse(File.Exists(oldLog));
    }
}
