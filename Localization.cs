using System;
using System.Globalization;

namespace BatteryHealth;

public enum AppLanguage
{
    Chinese,
    English
}

internal static class Localization
{
    public static AppLanguage CurrentLanguage { get; set; } =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Chinese
            : AppLanguage.English;

    public static bool IsChinese => CurrentLanguage == AppLanguage.Chinese;

    public static string WindowTitle => "Battery Health";
    public static string AppTitle => IsChinese ? "电池健康" : "Battery Health";
    public static string ToggleLanguageButton => IsChinese ? "English" : "中文";
    public static string BatteryInfoSection => IsChinese ? "电池信息" : "Battery Information";
    public static string FooterNote => IsChinese
        ? "数据只在本机读取，不联网、不驻留后台"
        : "Data is read locally only. No internet, no background residency.";

    public static string LabelBatteryName => IsChinese ? "电池名称" : "Battery Name";
    public static string LabelManufacturer => IsChinese ? "制造商" : "Manufacturer";
    public static string LabelDesignCapacity => IsChinese ? "设计容量" : "Design Capacity";
    public static string LabelFullChargeCapacity => IsChinese ? "当前满充容量" : "Full Charge Capacity";
    public static string LabelWear => IsChinese ? "损耗" : "Wear Level";
    public static string LabelCycleCount => IsChinese ? "循环次数" : "Cycle Count";
    public static string LabelChargePercent => IsChinese ? "当前电量" : "Current Charge";
    public static string LabelPowerState => IsChinese ? "当前状态" : "Power State";
    public static string LabelChargePower => IsChinese ? "充电功率" : "Charge Rate";

    public static string ChargePowerToolTip => IsChinese
        ? "实际进入电池的功率，每 5 秒更新；不代表充电器总输出。"
        : "Actual power entering the battery, updated every 5s; does not represent total charger output.";

    public static string StatusReading => IsChinese ? "正在读取电池…" : "Reading battery data…";
    public static string StatusNoBattery => IsChinese ? "未检测到电池" : "No battery detected";
    public static string StatusReadFailed => IsChinese ? "读取失败" : "Failed to read battery";

    public static string HealthLabel(double? health) => health switch
    {
        null => IsChinese ? "设备未提供容量信息" : "Capacity information unavailable",
        >= 80 => IsChinese ? "电池状态良好" : "Battery condition is good",
        >= 60 => IsChinese ? "电池已有明显损耗" : "Noticeable battery degradation",
        _ => IsChinese ? "建议检查或更换电池" : "Service or replacement recommended"
    };

    public static string NotAvailable => IsChinese ? "设备未提供" : "Not available";
    public static string UnknownBattery => IsChinese ? "未知电池" : "Unknown Battery";

    public static string FormatCycles(uint cycles) =>
        IsChinese ? $"{cycles:N0} 次" : $"{cycles:N0} cycles";

    public static string FormatPowerState(BatteryPowerState state) => state switch
    {
        BatteryPowerState.Charging => IsChinese ? "充电中" : "Charging",
        BatteryPowerState.Discharging => IsChinese ? "使用电池" : "On Battery",
        BatteryPowerState.PluggedIn => IsChinese ? "使用电源" : "Plugged In",
        _ => IsChinese ? "状态未知" : "Unknown"
    };
}
