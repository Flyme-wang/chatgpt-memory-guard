using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChatGPTMemoryGuard;

public static class IconFactory
{
    public static Icon Create(Color statusColor)
    {
        using var bitmap = new Bitmap(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var shadowBrush = new SolidBrush(Color.FromArgb(75, Color.Black));
        graphics.FillEllipse(shadowBrush, 3, 4, 27, 27);
        using var statusBrush = new SolidBrush(statusColor);
        graphics.FillEllipse(statusBrush, 2, 2, 27, 27);
        using var borderPen = new Pen(Color.FromArgb(210, Color.White), 2);
        graphics.DrawEllipse(borderPen, 3, 3, 25, 25);
        using var font = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("M", font, textBrush, new RectangleF(2, 1, 27, 27), format);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
