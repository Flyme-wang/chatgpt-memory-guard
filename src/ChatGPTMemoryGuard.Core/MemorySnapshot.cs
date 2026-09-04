namespace ChatGPTMemoryGuard.Core;

public sealed record MemorySnapshot(
    DateTimeOffset CapturedAt,
    int ProcessCount,
    long TotalWorkingSetBytes,
    long TotalPrivateBytes,
    long LargestPrivateBytes);

public enum AlertLevel
{
    Normal = 0,
    Warning = 1,
    Critical = 2,
}

public enum MemoryMetric
{
    TotalWorkingSet,
    LargestPrivateProcess,
}

public sealed record AlertDecision(
    AlertLevel Level,
    bool ShouldNotify,
    MemoryMetric TriggerMetric,
    long TriggeringBytes);
