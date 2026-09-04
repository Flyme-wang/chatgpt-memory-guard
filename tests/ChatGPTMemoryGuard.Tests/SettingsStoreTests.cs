using ChatGPTMemoryGuard;

namespace ChatGPTMemoryGuard.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    private string _directory = null!;
    private string _settingsPath = null!;

    [TestInitialize]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ChatGPTMemoryGuardTests", Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(_directory, "settings.json");
    }

    [TestCleanup]
    public void CleanUp()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void MissingFileCreatesDefaultSettings()
    {
        var store = new SettingsStore(_settingsPath);

        SettingsLoadResult result = store.Load();

        Assert.IsTrue(result.UsedDefaults);
        Assert.AreEqual(3.2, result.Settings.WarningGiB, 0.001);
        Assert.AreEqual(3.6, result.Settings.CriticalGiB, 0.001);
        Assert.AreEqual(5, result.Settings.PollIntervalSeconds);
        Assert.IsTrue(File.Exists(_settingsPath));
    }

    [TestMethod]
    public void SavedSettingsRoundTrip()
    {
        var store = new SettingsStore(_settingsPath);
        var settings = new GuardSettings
        {
            WarningGiB = 3.1,
            CriticalGiB = 3.7,
            PollIntervalSeconds = 8,
            CooldownMinutes = 12,
            ResetMarginMiB = 300,
        };

        store.Save(settings);
        SettingsLoadResult result = store.Load();

        Assert.IsFalse(result.UsedDefaults);
        Assert.AreEqual(settings, result.Settings);
    }

    [TestMethod]
    public void MalformedJsonFallsBackToDefaultsAndPreservesBadFile()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_settingsPath, "{ definitely not json");
        var store = new SettingsStore(_settingsPath);

        SettingsLoadResult result = store.Load();

        Assert.IsTrue(result.UsedDefaults);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(3.2, result.Settings.WarningGiB, 0.001);
        Assert.AreEqual(1, Directory.GetFiles(_directory, "settings.corrupt-*.json").Length);
        Assert.IsTrue(File.Exists(_settingsPath));
    }

    [TestMethod]
    public void InvalidValuesAreClampedAndWarningStaysBelowCritical()
    {
        var invalid = new GuardSettings
        {
            WarningGiB = 9,
            CriticalGiB = 2,
            PollIntervalSeconds = 0,
            CooldownMinutes = -1,
            ResetMarginMiB = -20,
        };

        GuardSettings valid = invalid.Validate();

        Assert.IsTrue(valid.WarningGiB < valid.CriticalGiB);
        Assert.IsTrue(valid.PollIntervalSeconds >= 1);
        Assert.IsTrue(valid.CooldownMinutes >= 1);
        Assert.IsTrue(valid.ResetMarginMiB >= 0);
    }
}
