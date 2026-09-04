using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard.Core.Tests;

[TestClass]
public sealed class MemorySnapshotBuilderTests
{
    [TestMethod]
    public void BuildAggregatesAllReadableProcesses()
    {
        var capturedAt = new DateTimeOffset(2026, 9, 4, 13, 0, 0, TimeSpan.Zero);
        ProcessMemorySample[] samples =
        [
            new(workingSetBytes: 100, privateBytes: 80),
            new(workingSetBytes: 200, privateBytes: 260),
            new(workingSetBytes: 300, privateBytes: 120),
        ];

        MemorySnapshot result = MemorySnapshotBuilder.Build(samples, capturedAt);

        Assert.AreEqual(capturedAt, result.CapturedAt);
        Assert.AreEqual(3, result.ProcessCount);
        Assert.AreEqual(600, result.TotalWorkingSetBytes);
        Assert.AreEqual(460, result.TotalPrivateBytes);
        Assert.AreEqual(260, result.LargestPrivateBytes);
    }

    [TestMethod]
    public void BuildSaturatesTotalsInsteadOfOverflowing()
    {
        ProcessMemorySample[] samples =
        [
            new(long.MaxValue - 10, long.MaxValue - 20),
            new(100, 100),
        ];

        MemorySnapshot result = MemorySnapshotBuilder.Build(samples, DateTimeOffset.UtcNow);

        Assert.AreEqual(long.MaxValue, result.TotalWorkingSetBytes);
        Assert.AreEqual(long.MaxValue, result.TotalPrivateBytes);
    }
}
