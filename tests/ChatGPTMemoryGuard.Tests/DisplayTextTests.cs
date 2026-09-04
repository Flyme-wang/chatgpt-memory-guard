using ChatGPTMemoryGuard;
using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard.Tests;

[TestClass]
public sealed class DisplayTextTests
{
    [TestMethod]
    public void TooltipReportsWhenChatGptIsNotRunning()
    {
        var snapshot = new MemorySnapshot(DateTimeOffset.Now, 0, 0, 0, 0);

        string text = DisplayText.BuildTooltip(snapshot);

        Assert.AreEqual("ChatGPT 未运行", text);
    }

    [TestMethod]
    public void StatusReportsTotalLargestAndProcessCount()
    {
        var snapshot = new MemorySnapshot(
            DateTimeOffset.Now,
            ProcessCount: 11,
            TotalWorkingSetBytes: Bytes(1.91),
            TotalPrivateBytes: Bytes(1.8),
            LargestPrivateBytes: Bytes(0.46));

        string text = DisplayText.BuildStatus(snapshot);

        StringAssert.Contains(text, "总计 1.91 GB");
        StringAssert.Contains(text, "最大进程 0.46 GB");
        StringAssert.Contains(text, "11 个进程");
    }

    [TestMethod]
    public void WarningNotificationAdvisesSavingWork()
    {
        var snapshot = new MemorySnapshot(DateTimeOffset.Now, 8, Bytes(3.25), Bytes(3.1), Bytes(1.2));

        NotificationText text = DisplayText.BuildNotification(AlertLevel.Warning, snapshot);

        Assert.AreEqual("ChatGPT 内存预警", text.Title);
        StringAssert.Contains(text.Message, "3.25 GB");
        StringAssert.Contains(text.Message, "保存当前工作");
    }

    private static long Bytes(double gibibytes) => (long)(gibibytes * 1024d * 1024d * 1024d);
}
