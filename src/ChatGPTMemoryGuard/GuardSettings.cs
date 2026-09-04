namespace ChatGPTMemoryGuard;

public sealed record GuardSettings
{
    public const double DefaultWarningGiB = 3.2;
    public const double DefaultCriticalGiB = 3.6;

    public double WarningGiB { get; init; } = DefaultWarningGiB;
    public double CriticalGiB { get; init; } = DefaultCriticalGiB;
    public int PollIntervalSeconds { get; init; } = 5;
    public int CooldownMinutes { get; init; } = 10;
    public int ResetMarginMiB { get; init; } = 256;
    public bool ShowOverlay { get; init; } = true;
    public int? OverlayLeft { get; init; }
    public int? OverlayTop { get; init; }

    public static GuardSettings Default { get; } = new();

    public GuardSettings Validate()
    {
        double warning = IsReasonable(WarningGiB) ? WarningGiB : DefaultWarningGiB;
        double critical = IsReasonable(CriticalGiB) ? CriticalGiB : DefaultCriticalGiB;
        if (critical <= warning)
        {
            warning = DefaultWarningGiB;
            critical = DefaultCriticalGiB;
        }

        int maximumResetMargin = Math.Max(0, (int)(warning * 512));
        return this with
        {
            WarningGiB = warning,
            CriticalGiB = critical,
            PollIntervalSeconds = Math.Clamp(PollIntervalSeconds, 1, 60),
            CooldownMinutes = Math.Clamp(CooldownMinutes, 1, 120),
            ResetMarginMiB = Math.Clamp(ResetMarginMiB, 0, maximumResetMargin),
        };
    }

    public long WarningBytes => GiBToBytes(WarningGiB);
    public long CriticalBytes => GiBToBytes(CriticalGiB);
    public long ResetMarginBytes => ResetMarginMiB * 1024L * 1024L;

    private static bool IsReasonable(double value) => double.IsFinite(value) && value is >= 0.5 and <= 64;

    private static long GiBToBytes(double value) => (long)(value * 1024d * 1024d * 1024d);
}
