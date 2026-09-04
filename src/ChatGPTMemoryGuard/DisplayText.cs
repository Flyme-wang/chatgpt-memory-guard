using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard;

public sealed record NotificationText(string Title, string Message);
public sealed record OverlayText(string Primary, string Secondary);

public static class DisplayText
{
    public static string BuildTooltip(MemorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ProcessCount == 0)
        {
            return "ChatGPT 未运行";
        }

        return $"ChatGPT 总计 {ToGiB(snapshot.TotalWorkingSetBytes):0.00} GB | 最大 {ToGiB(snapshot.LargestPrivateBytes):0.00} GB";
    }

    public static string BuildStatus(MemorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ProcessCount == 0)
        {
            return "状态：ChatGPT 未运行";
        }

        return $"总计 {ToGiB(snapshot.TotalWorkingSetBytes):0.00} GB | 最大进程 {ToGiB(snapshot.LargestPrivateBytes):0.00} GB | {snapshot.ProcessCount} 个进程";
    }

    public static NotificationText BuildNotification(AlertLevel level, MemorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        string title = level == AlertLevel.Critical ? "ChatGPT 内存紧急预警" : "ChatGPT 内存预警";
        string urgency = level == AlertLevel.Critical ? "已非常接近 4GB，请立即" : "正在接近 4GB，建议";
        string message = $"总计 {ToGiB(snapshot.TotalWorkingSetBytes):0.00} GB，最大进程 {ToGiB(snapshot.LargestPrivateBytes):0.00} GB。{urgency}保存当前工作，并在方便时手动重启 ChatGPT。";
        return new NotificationText(title, message);
    }

    public static OverlayText BuildOverlay(MemorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ProcessCount == 0)
        {
            return new OverlayText("ChatGPT 未运行", "等待应用启动");
        }

        return new OverlayText(
            $"总计 {ToGiB(snapshot.TotalWorkingSetBytes):0.00} GB",
            $"最大 {ToGiB(snapshot.LargestPrivateBytes):0.00} GB · {snapshot.ProcessCount} 进程");
    }

    public static string FormatGiB(long bytes) => $"{ToGiB(bytes):0.00} GB";

    private static double ToGiB(long bytes) => bytes / (1024d * 1024d * 1024d);
}
