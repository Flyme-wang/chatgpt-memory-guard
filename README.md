# ChatGPT Memory Guard

这是一个独立的 Windows 托盘小工具，用于监测 ChatGPT 桌面应用的内存占用，并在接近 4GB 前提醒你保存工作。它不会关闭、重启或修改 ChatGPT。

![桌面实时悬浮窗](overlay-preview.png)

## 直接使用

双击 `dist\ChatGPTMemoryGuard.exe`。程序启动后会显示可拖动的实时悬浮窗，右下角系统托盘也会出现带字母 **M** 的圆形图标：

- 灰色：ChatGPT 没有运行。
- 绿色：内存正常。
- 黄色：达到默认普通预警值 3.2GB。
- 红色：达到默认紧急预警值 3.6GB。

鼠标停在图标上可以看总内存和最大单进程内存。双击图标显示完整状态；右键可以立即刷新、设置随 Windows 启动、打开日志目录或退出。

## 桌面实时悬浮窗

启动后会在主屏幕右上角显示一个小型实时面板，内容每 5 秒更新：

- 第一行显示全部 ChatGPT 进程的总内存；
- 第二行显示最大单进程内存和进程数量；
- 底部细条表示当前占用相对 4GB 的比例；
- 绿色、黄色和红色分别对应正常、普通预警和紧急预警。

按住悬浮窗左键即可拖动，松开后自动记住位置。右键悬浮窗或托盘图标，选择“显示桌面悬浮窗”可以隐藏或重新显示。

## 内存口径

ChatGPT 桌面版由多个 `ChatGPT.exe` 子进程组成。工具同时检查：

1. 所有 ChatGPT 子进程的总工作集，反映它们当前占用的物理内存；
2. 最大单进程的私有内存，用于发现某个页面或渲染进程单独逼近限制。

任一数值达到预警阈值都会触发提醒。同一级别最多每 10 分钟提醒一次；从黄色升级到红色时会立即提醒。

## 安装到当前用户

在 PowerShell 中运行：

```powershell
.\scripts\install.ps1
```

需要安装后立即设置随 Windows 启动时运行：

```powershell
.\scripts\install.ps1 -EnableStartup
```

安装位置为 `%LOCALAPPDATA%\ChatGPTMemoryGuard`，不需要管理员权限。也可以不安装，直接运行 `dist` 中的程序。

## 卸载

```powershell
.\scripts\uninstall.ps1
```

默认保留设置和日志。需要一起删除时运行：

```powershell
.\scripts\uninstall.ps1 -RemoveData
```

## 设置和日志

- 设置：`%LOCALAPPDATA%\ChatGPTMemoryGuard\settings.json`
- 日志：`%LOCALAPPDATA%\ChatGPTMemoryGuard\logs`

首次启动会自动生成设置文件。普通用户只需通过托盘菜单启用或关闭开机启动。

## 运行要求

发布文件使用本机已安装的 .NET 8 Desktop Runtime。工具只能提前提醒，不能提高 ChatGPT 自身的内存上限；收到红色提醒时，应先保存正在编辑的内容，再手动重启 ChatGPT。
