using System.Drawing;
using ChatGPTMemoryGuard;
using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard.Tests;

[TestClass]
public sealed class FloatingOverlayTests
{
    [TestMethod]
    public void MissingSavedPositionUsesTopRightOfWorkingArea()
    {
        var workingArea = new Rectangle(0, 0, 1920, 1040);

        Point location = OverlayPlacement.Clamp(null, null, new Size(240, 72), workingArea);

        Assert.AreEqual(new Point(1664, 16), location);
    }

    [TestMethod]
    public void SavedPositionIsClampedInsideWorkingArea()
    {
        var workingArea = new Rectangle(100, 50, 1200, 700);

        Point location = OverlayPlacement.Clamp(5000, -100, new Size(240, 72), workingArea);

        Assert.AreEqual(new Point(1060, 50), location);
    }

    [TestMethod]
    public void OverlayTextShowsTotalLargestAndCount()
    {
        var snapshot = new MemorySnapshot(
            DateTimeOffset.Now,
            ProcessCount: 11,
            TotalWorkingSetBytes: Bytes(1.91),
            TotalPrivateBytes: Bytes(1.8),
            LargestPrivateBytes: Bytes(0.46));

        OverlayText text = DisplayText.BuildOverlay(snapshot);

        Assert.AreEqual("总计 1.91 GB", text.Primary);
        Assert.AreEqual("最大 0.46 GB · 11 进程", text.Secondary);
    }

    [TestMethod]
    public void OverlayIsVisibleByDefault()
    {
        Assert.IsTrue(GuardSettings.Default.ShowOverlay);
    }

    private static long Bytes(double gibibytes) => (long)(gibibytes * 1024d * 1024d * 1024d);
}
