$ErrorActionPreference = 'Stop'

dotnet build "$PSScriptRoot\BatteryHealth.csproj" -c Release

Write-Host "`n完成：$PSScriptRoot\bin\Release\net48\BatteryHealth.exe"
