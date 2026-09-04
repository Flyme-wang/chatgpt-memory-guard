$processes = @(Get-Process -Name 'ChatGPT' -ErrorAction SilentlyContinue)
$totalWorkingSet = ($processes | Measure-Object -Property WorkingSet64 -Sum).Sum
$totalPrivate = ($processes | Measure-Object -Property PrivateMemorySize64 -Sum).Sum
$largestPrivate = ($processes | Measure-Object -Property PrivateMemorySize64 -Maximum).Maximum
$totalWorkingSet = if ($null -eq $totalWorkingSet) { 0 } else { $totalWorkingSet }
$totalPrivate = if ($null -eq $totalPrivate) { 0 } else { $totalPrivate }
$largestPrivate = if ($null -eq $largestPrivate) { 0 } else { $largestPrivate }

[ordered]@{
    CapturedAt = (Get-Date).ToString('o')
    ProcessCount = $processes.Count
    TotalWorkingSetBytes = [long]$totalWorkingSet
    TotalWorkingSetGiB = [math]::Round(([double]$totalWorkingSet / 1GB), 3)
    TotalPrivateBytes = [long]$totalPrivate
    LargestPrivateBytes = [long]$largestPrivate
    LargestPrivateGiB = [math]::Round(([double]$largestPrivate / 1GB), 3)
} | ConvertTo-Json
