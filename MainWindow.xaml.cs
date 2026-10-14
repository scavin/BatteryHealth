using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BatteryHealth;

public partial class MainWindow : Window
{
    private enum ViewStatus
    {
        Reading,
        Loaded,
        NoBattery,
        Failed
    }

    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private ViewStatus viewStatus = ViewStatus.Reading;
    private BatterySnapshot? currentBattery;
    private string? lastErrorMessage;
    private bool isReading;
    private bool isClosed;

    public MainWindow()
    {
        InitializeComponent();
        ApplyLanguage();
        refreshTimer.Tick += async (_, _) => await LoadBatteryAsync();
        Loaded += async (_, _) =>
        {
            await LoadBatteryAsync();
            if (!isClosed) refreshTimer.Start();
        };
        Closed += (_, _) =>
        {
            isClosed = true;
            refreshTimer.Stop();
        };
    }

    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        Localization.CurrentLanguage = Localization.IsChinese
            ? AppLanguage.English
            : AppLanguage.Chinese;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Title = Localization.WindowTitle;
        TitleText.Text = Localization.AppTitle;
        LangButton.Content = Localization.ToggleLanguageButton;
        SectionTitleText.Text = Localization.BatteryInfoSection;
        FooterText.Text = Localization.FooterNote;

        LabelNameText.Text = Localization.LabelBatteryName;
        LabelManufacturerText.Text = Localization.LabelManufacturer;
        LabelDesignText.Text = Localization.LabelDesignCapacity;
        LabelFullText.Text = Localization.LabelFullChargeCapacity;
        LabelWearText.Text = Localization.LabelWear;
        LabelCycleText.Text = Localization.LabelCycleCount;
        LabelChargeText.Text = Localization.LabelChargePercent;
        LabelPowerText.Text = Localization.LabelPowerState;
        LabelChargePowerText.Text = Localization.LabelChargePower;
        ChargePowerValue.ToolTip = Localization.ChargePowerToolTip;

        RenderUI();
    }

    private async Task LoadBatteryAsync()
    {
        if (isReading || isClosed) return;
        isReading = true;
        try
        {
            var battery = await Task.Run(BatteryReader.Read);
            if (isClosed) return;
            if (battery is null)
            {
                viewStatus = ViewStatus.NoBattery;
                currentBattery = null;
                lastErrorMessage = null;
                RenderUI();
                return;
            }

            viewStatus = ViewStatus.Loaded;
            currentBattery = battery;
            lastErrorMessage = null;
            RenderUI();
        }
        catch (Exception ex)
        {
            if (isClosed) return;
            viewStatus = ViewStatus.Failed;
            currentBattery = null;
            lastErrorMessage = ex.Message;
            RenderUI();
        }
        finally { isReading = false; }
    }

    private void RenderUI()
    {
        switch (viewStatus)
        {
            case ViewStatus.Reading:
                HealthText.Text = "—";
                StatusText.Text = Localization.StatusReading;
                StatusText.ToolTip = null;
                HealthBar.Value = 0;
                ResetValues();
                break;

            case ViewStatus.NoBattery:
                ShowUnavailable(Localization.StatusNoBattery, null);
                break;

            case ViewStatus.Failed:
                ShowUnavailable(Localization.StatusReadFailed, lastErrorMessage);
                break;

            case ViewStatus.Loaded when currentBattery is not null:
                var health = currentBattery.HealthPercent;
                StatusText.ToolTip = null;
                HealthText.Text = health is null ? "—" : $"{health:0}%";
                HealthBar.Value = health ?? 0;
                StatusText.Text = Localization.HealthLabel(health);

                var color = health switch
                {
                    >= 80 => "#16A36A",
                    >= 60 => "#D58A00",
                    null => "#667085",
                    _ => "#D64545"
                };
                HealthText.Foreground = HealthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

                NameValue.Text = string.IsNullOrWhiteSpace(currentBattery.Name)
                    ? Localization.UnknownBattery
                    : currentBattery.Name;
                ManufacturerValue.Text = string.IsNullOrWhiteSpace(currentBattery.Manufacturer)
                    ? Localization.NotAvailable
                    : currentBattery.Manufacturer;
                DesignValue.Text = FormatCapacity(currentBattery.DesignedCapacity);
                FullValue.Text = FormatCapacity(currentBattery.FullChargedCapacity);
                WearValue.Text = currentBattery.WearCapacity is uint wear ? FormatCapacity(wear) : Localization.NotAvailable;
                CycleValue.Text = currentBattery.CycleCount is uint cycles ? Localization.FormatCycles(cycles) : Localization.NotAvailable;
                ChargeValue.Text = currentBattery.ChargePercent is int charge ? $"{charge}%" : Localization.NotAvailable;
                PowerValue.Text = Localization.FormatPowerState(currentBattery.PowerState);
                ChargePowerValue.Text = currentBattery.ChargePowerWatts is double watts
                    ? $"{watts:0.0} W" : Localization.NotAvailable;
                break;
        }
    }

    private void ShowUnavailable(string message, string? errorDetail)
    {
        HealthText.Text = "—";
        StatusText.Text = message;
        StatusText.ToolTip = errorDetail;
        HealthBar.Value = 0;
        ResetValues();
    }

    private void ResetValues()
    {
        NameValue.Text = ManufacturerValue.Text = DesignValue.Text = FullValue.Text =
            WearValue.Text = CycleValue.Text = ChargeValue.Text = PowerValue.Text =
            ChargePowerValue.Text = "—";
    }

    private static string FormatCapacity(uint? capacity) =>
        capacity is uint value ? $"{value:N0} mWh" : Localization.NotAvailable;
}
