using System.Diagnostics;
using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard;

public sealed class ProcessMemoryReader
{
    private readonly Action<string>? _reportError;

    public ProcessMemoryReader(Action<string>? reportError = null)
    {
        _reportError = reportError;
    }

    public MemorySnapshot Capture()
    {
        var samples = new List<ProcessMemorySample>();
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("ChatGPT");
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _reportError?.Invoke($"无法枚举 ChatGPT 进程：{error.Message}");
            return MemorySnapshotBuilder.Build(samples, DateTimeOffset.Now);
        }

        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    samples.Add(new ProcessMemorySample(process.WorkingSet64, process.PrivateMemorySize64));
                }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    _reportError?.Invoke($"跳过已退出或不可读取的 ChatGPT 进程：{error.Message}");
                }
            }
        }

        return MemorySnapshotBuilder.Build(samples, DateTimeOffset.Now);
    }
}
