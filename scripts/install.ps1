[CmdletBinding()]
param(
    [switch]$EnableStartup
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$sourceExe = Join-Path $projectRoot 'dist\ChatGPTMemoryGuard.exe'
$installDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ChatGPTMemoryGuard'
$installedExe = Join-Path $installDirectory 'ChatGPTMemoryGuard.exe'

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "找不到发布文件：$sourceExe"
}

New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $installedExe -Force

if ($EnableStartup) {
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    New-ItemProperty -Path $runKey -Name 'ChatGPTMemoryGuard' -Value ('"{0}"' -f $installedExe) -PropertyType String -Force | Out-Null
}

Start-Process -FilePath $installedExe
Write-Host "ChatGPT Memory Guard 已安装并启动：$installedExe"
