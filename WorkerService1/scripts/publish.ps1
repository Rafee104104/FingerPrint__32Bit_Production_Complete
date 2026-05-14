[CmdletBinding()]
param(
    [string]$ProjectPath,
    [string]$OutputPath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x86',
    [string]$ZkSdkDll
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
if (-not $ProjectPath) {
    $ProjectPath = Join-Path $scriptRoot '..\WorkerService1\WorkerService1.csproj'
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $scriptRoot '..\artifacts\publish\ZkK40OracleSync'
}

$projectFullPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $scriptRoot '..')).Path

if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $outputFullPath = $OutputPath
}
else {
    $outputFullPath = Join-Path $repoRoot $OutputPath
}

$outputFullPath = [System.IO.Path]::GetFullPath($outputFullPath)

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

dotnet publish $projectFullPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputFullPath

if ($ZkSdkDll) {
    $sdkFullPath = (Resolve-Path -LiteralPath $ZkSdkDll).Path
    Copy-Item -LiteralPath $sdkFullPath -Destination (Join-Path $outputFullPath 'zkemkeeper.dll') -Force
}

Write-Host "Published to $outputFullPath"
