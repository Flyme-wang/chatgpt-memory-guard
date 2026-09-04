namespace ChatGPTMemoryGuard;

public static class OverlayPlacement
{
    private const int DefaultMargin = 16;

    public static Point Clamp(int? savedLeft, int? savedTop, Size windowSize, Rectangle workingArea)
    {
        int maximumX = Math.Max(workingArea.Left, workingArea.Right - windowSize.Width);
        int maximumY = Math.Max(workingArea.Top, workingArea.Bottom - windowSize.Height);
        int defaultX = Math.Max(workingArea.Left, maximumX - DefaultMargin);
        int defaultY = Math.Min(maximumY, workingArea.Top + DefaultMargin);
        int x = Math.Clamp(savedLeft ?? defaultX, workingArea.Left, maximumX);
        int y = Math.Clamp(savedTop ?? defaultY, workingArea.Top, maximumY);
        return new Point(x, y);
    }
}
