using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BatteryHealth;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadBatteryAsync();
    }

    private async Task LoadBatteryAsync()
    {
        try
        {
            var battery = await Task.Run(BatteryReader.Read);
            if (battery is null)
            {
                ShowUnavailable("未检测到电池");
                return;
            }

            var health = battery.HealthPercent;
            HealthText.Text = health is null ? "—" : $"{health:0}%";
            HealthBar.Value = health ?? 0;
            StatusText.Text = HealthLabel(health);

            var color = health switch
            {
                >= 80 => "#16A36A",
                >= 60 => "#D58A00",
                null => "#667085",
                _ => "#D64545"
            };
            HealthText.Foreground = HealthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

            NameValue.Text = battery.Name;
            ManufacturerValue.Text = battery.Manufacturer;
            DesignValue.Text = FormatCapacity(battery.DesignedCapacity);
            FullValue.Text = FormatCapacity(battery.FullChargedCapacity);
            WearValue.Text = battery.WearCapacity is uint wear ? FormatCapacity(wear) : "设备未提供";
            CycleValue.Text = battery.CycleCount is uint cycles ? $"{cycles:N0} 次" : "设备未提供";
            ChargeValue.Text = battery.ChargePercent is int charge ? $"{charge}%" : "设备未提供";
            PowerValue.Text = battery.PowerState;
        }
        catch (Exception ex)
        {
            ShowUnavailable("读取失败");
            StatusText.ToolTip = ex.Message;
        }
    }

    private void ShowUnavailable(string message)
    {
        HealthText.Text = "—";
        StatusText.Text = message;
    }

    private static string HealthLabel(double? health) => health switch
    {
        null => "设备未提供容量信息",
        >= 80 => "电池状态良好",
        >= 60 => "电池已有明显损耗",
        _ => "建议检查或更换电池"
    };

    private static string FormatCapacity(uint? capacity) =>
        capacity is uint value ? $"{value:N0} mWh" : "设备未提供";

}
