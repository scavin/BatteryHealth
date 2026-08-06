using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System;
using System.Management;

namespace BatteryHealth;

internal sealed class BatterySnapshot
{
    public string Name { get; }
    public string Manufacturer { get; }
    public uint? DesignedCapacity { get; }
    public uint? FullChargedCapacity { get; }
    public uint? CycleCount { get; }
    public int? ChargePercent { get; }
    public string PowerState { get; }

    public BatterySnapshot(string name, string manufacturer, uint? designedCapacity,
        uint? fullChargedCapacity, uint? cycleCount, int? chargePercent, string powerState)
    {
        Name = name;
        Manufacturer = manufacturer;
        DesignedCapacity = designedCapacity;
        FullChargedCapacity = fullChargedCapacity;
        CycleCount = cycleCount;
        ChargePercent = chargePercent;
        PowerState = powerState;
    }

    public double? HealthPercent => DesignedCapacity > 0 && FullChargedCapacity.HasValue
        ? Math.Min(100, FullChargedCapacity.Value * 100.0 / DesignedCapacity.Value)
        : (double?)null;

    public uint? WearCapacity => DesignedCapacity is uint design && FullChargedCapacity is uint full
        ? design > full ? design - full : 0
        : null;
}

internal static class BatteryReader
{
    private static readonly Guid BatteryClassGuid = new("72631E54-78A4-11D0-BCF7-00AA00B7B32A");
    private const uint DigcfPresent = 0x2;
    private const uint DigcfDeviceInterface = 0x10;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 1;
    private const uint FileShareWrite = 2;
    private const uint OpenExisting = 3;
    private const uint IoctlQueryTag = 0x00294040;
    private const uint IoctlQueryInformation = 0x00294044;
    private const uint IoctlQueryStatus = 0x0029404C;

    public static BatterySnapshot? Read()
    {
        try
        {
            var native = ReadNative();
            if (native is not null) return native;
        }
        catch (Exception)
        {
            // Some OEM battery drivers expose data to WMI but reject direct IOCTL queries.
        }

        return ReadWmi();
    }

    private static BatterySnapshot? ReadNative()
    {
        var batteryClassGuid = BatteryClassGuid;
        var set = SetupDiGetClassDevs(ref batteryClassGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (set == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            for (uint index = 0; ; index++)
            {
                var data = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref batteryClassGuid, index, ref data))
                {
                    if (Marshal.GetLastWin32Error() == 259) break;
                    continue;
                }

                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out var needed, IntPtr.Zero);
                var detail = Marshal.AllocHGlobal((int)needed);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, detail, needed, out _, IntPtr.Zero)) continue;
                    var path = Marshal.PtrToStringUni(detail + 4) ?? string.Empty;
                    using var handle = OpenBattery(path);
                    if (handle.IsInvalid) continue;

                    var snapshot = ReadFromHandle(handle);
                    if (snapshot is not null) return snapshot;
                }
                finally { Marshal.FreeHGlobal(detail); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }

        return null;
    }

    private static SafeFileHandle OpenBattery(string path)
    {
        var handle = CreateFile(path, GenericRead | GenericWrite, FileShareRead | FileShareWrite,
            IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (!handle.IsInvalid) return handle;

        handle.Dispose();
        return CreateFile(path, GenericRead, FileShareRead | FileShareWrite,
            IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
    }

    private static BatterySnapshot? ReadWmi()
    {
        try
        {
            var battery = QueryFirst(@"root\cimv2", "SELECT Name, Status, EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery");
            if (battery is null) return null;

            var name = Convert.ToString(battery["Name"]) ?? "未知电池";
            var charge = ToInt(battery["EstimatedChargeRemaining"]);
            var batteryStatus = ToInt(battery["BatteryStatus"]);
            battery.Dispose();

            var staticData = QueryFirst(@"root\wmi", "SELECT DesignedCapacity, DeviceName, ManufactureName FROM BatteryStaticData");
            var designed = ToUInt(staticData?["DesignedCapacity"]);
            name = Convert.ToString(staticData?["DeviceName"]) ?? name;
            var manufacturer = Convert.ToString(staticData?["ManufactureName"]) ?? "设备未提供";
            staticData?.Dispose();

            var fullData = QueryFirst(@"root\wmi", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
            var full = ToUInt(fullData?["FullChargedCapacity"]);
            fullData?.Dispose();

            var cycleData = QueryFirst(@"root\wmi", "SELECT CycleCount FROM BatteryCycleCount");
            var cycles = ToUInt(cycleData?["CycleCount"]);
            cycleData?.Dispose();

            return new BatterySnapshot(name, manufacturer, designed, full, cycles, charge, WmiPowerState(batteryStatus));
        }
        catch (ManagementException)
        {
            return null;
        }
    }

    private static ManagementObject? QueryFirst(string scope, string query)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject item in searcher.Get()) return item;
            return null;
        }
        catch (ManagementException)
        {
            return null;
        }
    }

    private static uint? ToUInt(object? value)
    {
        if (value is null) return null;
        try
        {
            var converted = Convert.ToUInt32(value);
            return ValidCapacity(converted);
        }
        catch (Exception) { return null; }
    }

    private static int? ToInt(object? value)
    {
        if (value is null) return null;
        try { return Convert.ToInt32(value); }
        catch (Exception) { return null; }
    }

    private static string WmiPowerState(int? status) => status switch
    {
        1 => "使用电池",
        2 or 3 or 11 => "使用电源",
        >= 6 and <= 9 => "充电中",
        _ => "状态未知"
    };

    private static BatterySnapshot? ReadFromHandle(SafeFileHandle handle)
    {
        uint wait = 0;
        if (!DeviceIoControl(handle, IoctlQueryTag, ref wait, sizeof(uint), out uint tag, sizeof(uint), out _, IntPtr.Zero) || tag == 0)
            return null;

        var query = new BATTERY_QUERY_INFORMATION { BatteryTag = tag, InformationLevel = BatteryQueryInformationLevel.BatteryInformation };
        if (!DeviceIoControl(handle, IoctlQueryInformation, ref query, Marshal.SizeOf<BATTERY_QUERY_INFORMATION>(),
            out BATTERY_INFORMATION info, Marshal.SizeOf<BATTERY_INFORMATION>(), out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        var name = QueryString(handle, tag, BatteryQueryInformationLevel.BatteryDeviceName) ?? "未知电池";
        var manufacturer = QueryString(handle, tag, BatteryQueryInformationLevel.BatteryManufactureName) ?? "设备未提供";

        var waitStatus = new BATTERY_WAIT_STATUS { BatteryTag = tag };
        BATTERY_STATUS? status = null;
        if (DeviceIoControl(handle, IoctlQueryStatus, ref waitStatus, Marshal.SizeOf<BATTERY_WAIT_STATUS>(),
            out BATTERY_STATUS batteryStatus, Marshal.SizeOf<BATTERY_STATUS>(), out _, IntPtr.Zero))
            status = batteryStatus;

        var charge = status is { Capacity: var current } && info.FullChargedCapacity > 0
            ? (int)Math.Max(0, Math.Min(100, current * 100.0 / info.FullChargedCapacity))
            : (int?)null;

        var state = status?.PowerState switch
        {
            uint s when (s & 0x4) != 0 => "充电中",
            uint s when (s & 0x2) != 0 => "使用电池",
            uint s when (s & 0x1) != 0 => "使用电源",
            _ => "状态未知"
        };

        return new BatterySnapshot(name, manufacturer,
            ValidCapacity(info.DesignedCapacity), ValidCapacity(info.FullChargedCapacity),
            info.CycleCount == uint.MaxValue ? null : info.CycleCount, charge, state);
    }

    private static uint? ValidCapacity(uint value) => value is 0 or uint.MaxValue ? null : value;

    private static string? QueryString(SafeFileHandle handle, uint tag, BatteryQueryInformationLevel level)
    {
        var query = new BATTERY_QUERY_INFORMATION { BatteryTag = tag, InformationLevel = level };
        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            if (!DeviceIoControl(handle, IoctlQueryInformation, ref query, Marshal.SizeOf<BATTERY_QUERY_INFORMATION>(),
                buffer, 512, out _, IntPtr.Zero)) return null;
            return Marshal.PtrToStringUni(buffer)?.TrimEnd('\0');
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private enum BatteryQueryInformationLevel { BatteryInformation, BatteryGranularityInformation, BatteryTemperature, BatteryEstimatedTime, BatteryDeviceName, BatteryManufactureDate, BatteryManufactureName }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_QUERY_INFORMATION { public uint BatteryTag; public BatteryQueryInformationLevel InformationLevel; public int AtRate; }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct BATTERY_INFORMATION
    {
        public uint Capabilities;
        public byte Technology;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public byte[] Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] Chemistry;
        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_WAIT_STATUS { public uint BatteryTag; public uint Timeout; public uint PowerState; public uint LowCapacity; public uint HighCapacity; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_STATUS { public uint PowerState; public uint Capacity; public uint Voltage; public int Rate; }

    [DllImport("setupapi.dll", SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref SP_DEVICE_INTERFACE_DATA data);
    [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref SP_DEVICE_INTERFACE_DATA data, IntPtr detail, uint size, out uint needed, IntPtr deviceInfo);
    [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, ref uint input, int inputSize, out uint output, int outputSize, out uint returned, IntPtr overlapped);
    [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)] private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, ref BATTERY_QUERY_INFORMATION input, int inputSize, out BATTERY_INFORMATION output, int outputSize, out uint returned, IntPtr overlapped);
    [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)] private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, ref BATTERY_QUERY_INFORMATION input, int inputSize, IntPtr output, int outputSize, out uint returned, IntPtr overlapped);
    [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)] private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, ref BATTERY_WAIT_STATUS input, int inputSize, out BATTERY_STATUS output, int outputSize, out uint returned, IntPtr overlapped);
}
