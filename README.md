# Battery Health

一个极简的 Windows 电池健康查看器。打开后立即读取 Windows Battery Class Driver，显示健康度；展开后可查看容量、损耗、循环次数等信息。

## 构建

构建电脑需要 Windows、Visual Studio 2022，或 .NET SDK 加上 .NET Framework 4.8 Developer Pack。

在 PowerShell 中运行：

```powershell
.\build.ps1
```

每次推送到 `main`、提交 Pull Request 或手动触发 GitHub Actions 时，也会在 Windows 环境自动编译。构建结果可在对应 Actions 运行页面的 Artifacts 中下载。

生成的 `BatteryHealth.exe` 是小体积单文件程序，不需要管理员权限。普通 Windows 10/11 通常已包含 .NET Framework 4.8，因此用户不需要安装 SDK 或额外运行库。

## 说明

- 不联网，不写注册表，不驻留后台。
- 健康度 = 当前满充容量 ÷ 设计容量。
- Windows 设备支持程度取决于电池固件；缺失字段会显示“设备未提供”。
- 当前版本读取系统枚举到的第一块电池。
- 目标系统：Windows 10/11。精简系统若移除了 .NET Framework 组件，需要重新启用该系统组件。
