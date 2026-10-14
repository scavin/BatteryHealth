# Battery Health

[English](README.md)

一个极简的 Windows 电池健康查看器。打开后立即读取 Windows Battery Class Driver，显示健康度；展开后可查看容量、损耗、循环次数、充放电速率等信息。

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
- 窗口打开时每 5 秒刷新数据，关闭后停止读取。
- 充电功率来自 Windows 电池驱动的充放电速率，单位为 W，表示进入电池的功率，不代表充电器总输出；有效速率为零或正在放电时显示 `0.0 W`。
- 驱动未提供速率、相对单位缺少电压数据，或底层未暴露功率数据时，充电功率显示“设备未提供”。部分设备只提供放电速率，充电时可能无法显示功率。
- 支持中英文双语界面：跟随系统语言自动显示（非中文环境默认英文），也可在窗口右上角随时点击切换。
- 目标系统：Windows 10/11。精简系统若移除了 .NET Framework 组件，需要重新启用该系统组件。

## 许可证

BatteryHealth 使用 [GNU Affero General Public License v3.0](LICENSE) 授权，采用 `AGPL-3.0-only`：你可以使用、研究、修改和分发本软件；分发修改版本或通过网络向用户提供修改版本的功能时，需要按照许可证提供对应源代码。
