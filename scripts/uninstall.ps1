[CmdletBinding()]
param(
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$installDirectory = [IO.Path]::GetFullPath((Join-Path $localAppData 'ChatGPTMemoryGuard'))
$installedExe = Join-Path $installDirectory 'ChatGPTMemoryGuard.exe'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Get-Process -Name 'ChatGPTMemoryGuard' -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path $runKey -Name 'ChatGPTMemoryGuard' -ErrorAction SilentlyContinue

if ($RemoveData) {
    $allowedRoot = [IO.Path]::GetFullPath($localAppData).TrimEnd('\') + '\'
    if (-not $installDirectory.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝删除预期目录以外的路径：$installDirectory"
    }
    if (Test-Path -LiteralPath $installDirectory) {
        Remove-Item -LiteralPath $installDirectory -Recurse -Force
    }
} elseif (Test-Path -LiteralPath $installedExe) {
    Remove-Item -LiteralPath $installedExe -Force
}

Write-Host 'ChatGPT Memory Guard 已卸载。'
