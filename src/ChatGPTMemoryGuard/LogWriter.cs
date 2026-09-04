using System.Text;

namespace ChatGPTMemoryGuard;

public sealed class LogWriter
{
    private readonly string _logDirectory;
    private readonly int _retentionDays;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _sync = new();

    public LogWriter(string logDirectory, int retentionDays = 14, Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);
        _logDirectory = Path.GetFullPath(logDirectory);
        _retentionDays = retentionDays;
        _clock = clock ?? (() => DateTimeOffset.Now);
        EnsureDirectoryAndPrune();
    }

    public string DirectoryPath => _logDirectory;

    public void Write(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(_logDirectory);
                DateTimeOffset now = _clock();
                string path = Path.Combine(_logDirectory, $"guard-{now:yyyy-MM-dd}.log");
                File.AppendAllText(path, $"{now:yyyy-MM-dd HH:mm:ss zzz}  {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Logging must never stop memory monitoring.
        }
    }

    private void EnsureDirectoryAndPrune()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            DateTime cutoffUtc = _clock().UtcDateTime.AddDays(-_retentionDays);
            foreach (string path in Directory.EnumerateFiles(_logDirectory, "guard-*.log"))
            {
                if (File.GetLastWriteTimeUtc(path) < cutoffUtc)
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Cleanup is best-effort and must not stop startup.
        }
    }
}
