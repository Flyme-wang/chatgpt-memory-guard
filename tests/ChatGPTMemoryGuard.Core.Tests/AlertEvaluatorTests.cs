using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard.Core.Tests;

[TestClass]
public sealed class AlertEvaluatorTests
{
    private const long GiB = 1024L * 1024L * 1024L;
    private static readonly DateTimeOffset Start = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void UnderWarningThresholdDoesNotNotify()
    {
        var evaluator = CreateEvaluator();

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.19, 1.0), Start);

        Assert.AreEqual(AlertLevel.Normal, decision.Level);
        Assert.IsFalse(decision.ShouldNotify);
    }

    [TestMethod]
    public void ReachingWarningThresholdNotifies()
    {
        var evaluator = CreateEvaluator();

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.2, 1.0), Start);

        Assert.AreEqual(AlertLevel.Warning, decision.Level);
        Assert.IsTrue(decision.ShouldNotify);
        Assert.AreEqual(MemoryMetric.TotalWorkingSet, decision.TriggerMetric);
    }

    [TestMethod]
    public void LargestPrivateProcessCanTriggerCriticalAlert()
    {
        var evaluator = CreateEvaluator();

        AlertDecision decision = evaluator.Evaluate(Snapshot(2.0, 3.6), Start);

        Assert.AreEqual(AlertLevel.Critical, decision.Level);
        Assert.IsTrue(decision.ShouldNotify);
        Assert.AreEqual(MemoryMetric.LargestPrivateProcess, decision.TriggerMetric);
    }

    [TestMethod]
    public void EscalatingFromWarningToCriticalNotifiesImmediately()
    {
        var evaluator = CreateEvaluator();
        evaluator.Evaluate(Snapshot(3.2, 1.0), Start);

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.6, 1.0), Start.AddSeconds(5));

        Assert.AreEqual(AlertLevel.Critical, decision.Level);
        Assert.IsTrue(decision.ShouldNotify);
    }

    [TestMethod]
    public void SameLevelWithinCooldownDoesNotNotify()
    {
        var evaluator = CreateEvaluator();
        evaluator.Evaluate(Snapshot(3.2, 1.0), Start);

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.3, 1.0), Start.AddMinutes(9));

        Assert.AreEqual(AlertLevel.Warning, decision.Level);
        Assert.IsFalse(decision.ShouldNotify);
    }

    [TestMethod]
    public void SameLevelAfterCooldownNotifiesAgain()
    {
        var evaluator = CreateEvaluator();
        evaluator.Evaluate(Snapshot(3.2, 1.0), Start);

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.3, 1.0), Start.AddMinutes(10));

        Assert.AreEqual(AlertLevel.Warning, decision.Level);
        Assert.IsTrue(decision.ShouldNotify);
    }

    [TestMethod]
    public void DroppingBelowResetMarginRearmsWarningNotification()
    {
        var evaluator = CreateEvaluator();
        evaluator.Evaluate(Snapshot(3.2, 1.0), Start);
        evaluator.Evaluate(Snapshot(2.9, 1.0), Start.AddMinutes(1));

        AlertDecision decision = evaluator.Evaluate(Snapshot(3.2, 1.0), Start.AddMinutes(2));

        Assert.AreEqual(AlertLevel.Warning, decision.Level);
        Assert.IsTrue(decision.ShouldNotify);
    }

    private static AlertEvaluator CreateEvaluator() => new(
        warningBytes: Bytes(3.2),
        criticalBytes: Bytes(3.6),
        cooldown: TimeSpan.FromMinutes(10),
        resetMarginBytes: 256L * 1024L * 1024L);

    private static MemorySnapshot Snapshot(double totalWorkingSetGiB, double largestPrivateGiB) => new(
        CapturedAt: Start,
        ProcessCount: 3,
        TotalWorkingSetBytes: Bytes(totalWorkingSetGiB),
        TotalPrivateBytes: Bytes(totalWorkingSetGiB),
        LargestPrivateBytes: Bytes(largestPrivateGiB));

    private static long Bytes(double gibibytes) => (long)(gibibytes * GiB);
}
