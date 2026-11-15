<#
.SYNOPSIS
    Build TogetherLobbyJoin (a BepInEx 5 plugin for the Together / CasualtiesMP multiplayer mod)
    and optionally deploy it into the game.

.DESCRIPTION
    The project references assemblies that ship with the game, so the game folder must be
    provided (or auto-detected in the usual Steam libraries).

    GameDir can be given explicitly:
        pwsh -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Casualties Unknown"

    Output:
        dist\TogetherLobbyJoin.dll

.EXAMPLE
    pwsh -File .\build.ps1
    pwsh -File .\build.ps1 -NoDeploy
    pwsh -File .\build.ps1 -GameDir "C:\Games\Casualties Unknown" -SkipVerify
#>
[CmdletBinding()]
param(
    [string]$GameDir = "",
    [switch]$NoDeploy,
    [switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-DotNet {
    $candidates = @(
        $env:DOTNET_ROOT,
        "$env:ProgramFiles\dotnet",
        "$env:LOCALAPPDATA\Microsoft\dotnet",
        'F:\dotnet'
    ) | Where-Object { $_ -and (Test-Path (Join-Path $_ 'dotnet.exe')) }

    if (@($candidates).Count -gt 0) { return (Join-Path @($candidates)[0] 'dotnet.exe') }

    $cmd = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    throw "dotnet.exe not found. Install the .NET SDK (8.0 or newer) or set DOTNET_ROOT."
}

function Find-GameDir {
    if ($env:CASUALTIES_DIR -and (Test-Path (Join-Path $env:CASUALTIES_DIR 'CasualtiesUnknown.exe'))) {
        return $env:CASUALTIES_DIR
    }

    $commonDirs = New-Object System.Collections.Generic.List[string]

    foreach ($pf in @("$env:ProgramFiles\Steam", "${env:ProgramFiles(x86)}\Steam")) {
        if ($pf) { $commonDirs.Add((Join-Path $pf 'steamapps\common')) }
    }

    # every drive: <drive>\SteamLibrary, <drive>\Steam, <drive>\SteamLibrary\steamapps\common ...
    foreach ($d in (Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Root -match '^[A-Za-z]:\\$' })) {
        foreach ($sub in @('SteamLibrary', 'Steam', 'Program Files\Steam', 'Program Files (x86)\Steam', 'Games\Steam')) {
            $p = Join-Path $d.Root "$sub\steamapps\common"
            if (Test-Path $p) { $commonDirs.Add($p) }
        }
    }

    # libraries declared in libraryfolders.vdf
    $vfds = @()
    foreach ($c in $commonDirs) {
        $vdf = Join-Path (Split-Path $c -Parent) 'libraryfolders.vdf'
        if (Test-Path $vdf) { $vfds += $vdf }
    }
    foreach ($vdf in $vfds) {
        foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
            $lib = $m.Groups[1].Value -replace '\\\\', '\'
            $commonDirs.Add((Join-Path $lib 'steamapps\common'))
        }
    }

    foreach ($c in $commonDirs) {
        foreach ($name in @('Casualties Unknown', 'Casualties Unknown Demo')) {
            $candidate = Join-Path $c $name
            if (Test-Path (Join-Path $candidate 'CasualtiesUnknown.exe')) { return $candidate }
        }
    }

    return ''
}

$dotnet = Find-DotNet
$env:DOTNET_ROOT = Split-Path -Parent $dotnet
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not $GameDir) {
    throw 'Game folder not found. Pass -GameDir "<SteamLibrary>\steamapps\common\Casualties Unknown" or set the CASUALTIES_DIR environment variable.'
}

$proj = Join-Path $root 'src\TogetherLobbyJoin\TogetherLobbyJoin.csproj'
$buildArgs = @('build', $proj, '-c', 'Release', '-v', 'minimal', "-p:GameDir=$GameDir")

Write-Host "==> dotnet $($buildArgs -join ' ')" -ForegroundColor Cyan
& $dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "build failed (exit $LASTEXITCODE)" }

$dll = Join-Path $root 'dist\TogetherLobbyJoin.dll'
if (-not (Test-Path $dll)) { throw "missing output: $dll" }
Write-Host "==> output: $dll" -ForegroundColor Green

if (-not $SkipVerify) {
    Write-Host "==> reflection verification (checks the mod's internals this plugin depends on)" -ForegroundColor Cyan
    Push-Location (Join-Path $root 'tools\verify')
    try {
        & $dotnet run -c Release -- $GameDir $dll
        if ($LASTEXITCODE -ne 0) { Write-Warning "verification reported failures (see output above)." }
    }
    finally { Pop-Location }
}

if ($NoDeploy) { return }

$target = Join-Path $GameDir 'BepInEx\plugins\Together'
if (-not (Test-Path $target)) {
    Write-Warning "BepInEx\plugins\Together not found in $GameDir - is the Together mod installed?"
    return
}

Copy-Item $dll (Join-Path $target 'TogetherLobbyJoin.dll') -Force
Write-Host "==> deployed: $(Join-Path $target 'TogetherLobbyJoin.dll')" -ForegroundColor Green
