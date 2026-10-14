# Battery Health

[简体中文](readme-cn.md)

A minimalistic battery health viewer for Windows. Reads from the Windows Battery Class Driver immediately on launch to display battery health; also provides detailed battery information including capacity, wear level, cycle count, and charging rate.

## Build

Building requires Windows, Visual Studio 2022, or the .NET SDK with .NET Framework 4.8 Developer Pack.

Run in PowerShell:

```powershell
.\build.ps1
```

A Windows build is also automatically compiled via GitHub Actions on every push to `main`, Pull Request, or manual workflow dispatch. Build artifacts can be downloaded from the corresponding GitHub Actions run page.

The generated `BatteryHealth.exe` is a lightweight, single-file executable that requires no administrator privileges. Standard Windows 10/11 installations already include .NET Framework 4.8, so users do not need to install SDKs or extra runtimes.

## Features & Notes

- No internet access, no registry changes, and no background residency.
- Battery Health = Full Charge Capacity ÷ Design Capacity.
- Device support depends on battery firmware; unavailable fields will display "Not available".
- Currently reads the first battery enumerated by the system.
- Automatically refreshes data every 5 seconds while the window is open, and stops reading when closed.
- Charge rate is retrieved from the Windows battery driver rate metric in Watts (W), indicating actual power entering the battery, which does not represent total charger output. Displays `0.0 W` when not charging (idle or discharging).
- If the driver does not provide rate metrics, uses relative units without voltage data, or does not expose power data, "Not available" is displayed. Some devices only report discharge rates and may not display charge rate while charging.
- Bilingual interface (English / Simplified Chinese): automatically adapts to system language (defaults to English on non-Chinese systems) and can be toggled anytime via the button in the top-right corner.
- Target OS: Windows 10/11. If using a stripped-down/lite Windows edition where .NET Framework 4.8 was removed, re-enable this system component first.

## License

BatteryHealth is licensed under the [GNU Affero General Public License v3.0](LICENSE) (`AGPL-3.0-only`): you are free to use, study, modify, and distribute this software; if you distribute a modified version or provide modified functionality over a network, you must make the corresponding source code available under the terms of the license.
