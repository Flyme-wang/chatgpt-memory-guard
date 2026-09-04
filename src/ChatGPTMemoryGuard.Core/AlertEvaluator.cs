namespace ChatGPTMemoryGuard.Core;

public sealed class AlertEvaluator
{
    private readonly long _warningBytes;
    private readonly long _criticalBytes;
    private readonly TimeSpan _cooldown;
    private readonly long _resetMarginBytes;
    private AlertLevel _lastNotifiedLevel = AlertLevel.Normal;
    private DateTimeOffset? _lastNotificationAt;

    public AlertEvaluator(long warningBytes, long criticalBytes, TimeSpan cooldown, long resetMarginBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warningBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(criticalBytes, warningBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cooldown, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(resetMarginBytes);

        _warningBytes = warningBytes;
        _criticalBytes = criticalBytes;
        _cooldown = cooldown;
        _resetMarginBytes = resetMarginBytes;
    }

    public AlertDecision Evaluate(MemorySnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        MemoryMetric metric = snapshot.TotalWorkingSetBytes >= snapshot.LargestPrivateBytes
            ? MemoryMetric.TotalWorkingSet
            : MemoryMetric.LargestPrivateProcess;
        long triggeringBytes = Math.Max(snapshot.TotalWorkingSetBytes, snapshot.LargestPrivateBytes);
        AlertLevel level = triggeringBytes >= _criticalBytes
            ? AlertLevel.Critical
            : triggeringBytes >= _warningBytes
                ? AlertLevel.Warning
                : AlertLevel.Normal;

        if (level == AlertLevel.Normal)
        {
            if (triggeringBytes <= _warningBytes - _resetMarginBytes)
            {
                _lastNotificationAt = null;
                _lastNotifiedLevel = AlertLevel.Normal;
            }

            return new AlertDecision(level, false, metric, triggeringBytes);
        }

        bool shouldNotify = _lastNotificationAt is null
            || level > _lastNotifiedLevel
            || now - _lastNotificationAt.Value >= _cooldown;

        if (shouldNotify)
        {
            _lastNotificationAt = now;
            _lastNotifiedLevel = level;
        }

        return new AlertDecision(level, shouldNotify, metric, triggeringBytes);
    }
}
