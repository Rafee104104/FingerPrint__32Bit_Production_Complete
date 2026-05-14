[CmdletBinding()]
param(
    [string]$ServiceName = 'ZkK40OracleSync',
    [string]$DisplayName = 'ZKTeco K40 Oracle Attendance Sync',
    [string]$SourcePath,
    [string]$InstallPath = 'C:\Services\ZkK40OracleSync',
    [string]$ExeName = 'ZkK40OracleSync.exe',
    [string]$ZkSdkDll,
    [switch]$Start
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ($PSScriptRoot) {
    $PSScriptRoot
}
elseif ($PSCommandPath) {
    Split-Path -Parent $PSCommandPath
}
else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
if (-not $SourcePath) {
    $SourcePath = Join-Path $scriptRoot '..\artifacts\publish\ZkK40OracleSync'
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}

$sourceFullPath = (Resolve-Path -LiteralPath $SourcePath).Path
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService -and $existingService.Status -ne 'Stopped') {
    Stop-Service -Name $ServiceName -Force
    $existingService.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

Copy-Item -Path (Join-Path $sourceFullPath '*') -Destination $InstallPath -Recurse -Force

if ($ZkSdkDll) {
    $sdkFullPath = (Resolve-Path -LiteralPath $ZkSdkDll).Path
    Copy-Item -LiteralPath $sdkFullPath -Destination (Join-Path $InstallPath 'zkemkeeper.dll') -Force
}

$installedSdkDll = Join-Path $InstallPath 'zkemkeeper.dll'
if (Test-Path -LiteralPath $installedSdkDll) {
    & "$env:WINDIR\SysWOW64\regsvr32.exe" /s $installedSdkDll
}
else {
    Write-Warning 'zkemkeeper.dll was not found in the install folder. Register the x86 ZKTeco SDK before starting the service.'
}

$exePath = Join-Path $InstallPath $ExeName
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Service executable was not found: $exePath"
}

$binaryPath = "`"$exePath`""

if ($existingService) {
    & sc.exe config $ServiceName binPath= $binaryPath start= auto DisplayName= $DisplayName | Out-Null
}
else {
    New-Service -Name $ServiceName -BinaryPathName $binaryPath -DisplayName $DisplayName -StartupType Automatic | Out-Null
}

& sc.exe description $ServiceName 'Reads ZKTeco K40 attendance logs and syncs them to Oracle.' | Out-Null

if ($Start) {
    Start-Service -Name $ServiceName
}

Write-Host "Installed Windows service $ServiceName from $InstallPath"
