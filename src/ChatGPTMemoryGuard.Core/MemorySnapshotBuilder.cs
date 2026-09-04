namespace ChatGPTMemoryGuard.Core;

public readonly record struct ProcessMemorySample
{
    public ProcessMemorySample(long workingSetBytes, long privateBytes)
    {
        WorkingSetBytes = Math.Max(0, workingSetBytes);
        PrivateBytes = Math.Max(0, privateBytes);
    }

    public long WorkingSetBytes { get; }
    public long PrivateBytes { get; }
}

public static class MemorySnapshotBuilder
{
    public static MemorySnapshot Build(IEnumerable<ProcessMemorySample> samples, DateTimeOffset capturedAt)
    {
        ArgumentNullException.ThrowIfNull(samples);

        int count = 0;
        long totalWorkingSet = 0;
        long totalPrivate = 0;
        long largestPrivate = 0;

        foreach (ProcessMemorySample sample in samples)
        {
            count++;
            totalWorkingSet = SaturatingAdd(totalWorkingSet, sample.WorkingSetBytes);
            totalPrivate = SaturatingAdd(totalPrivate, sample.PrivateBytes);
            largestPrivate = Math.Max(largestPrivate, sample.PrivateBytes);
        }

        return new MemorySnapshot(capturedAt, count, totalWorkingSet, totalPrivate, largestPrivate);
    }

    private static long SaturatingAdd(long left, long right) =>
        left > long.MaxValue - right ? long.MaxValue : left + right;
}
