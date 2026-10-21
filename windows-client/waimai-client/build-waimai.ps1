param(
    [switch]$RunSelfTest,
    [switch]$RenderScreenshots
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$CsprojPath = Join-Path $ProjectDir "WaimaiClient.csproj"
$MsBuildExe = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $MsBuildExe)) {
    throw "MSBuild.exe not found at $MsBuildExe"
}

Write-Host "[1/3] Building MeituanWaimaiClient.exe (Release|AnyCPU)..." -ForegroundColor Cyan
& $MsBuildExe $CsprojPath /p:Configuration=Release /p:Platform=AnyCPU /nologo /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

$ExePath = Join-Path $ProjectDir "bin\Release\MeituanWaimaiClient.exe"
if (-not (Test-Path $ExePath)) {
    throw "Compiled executable not found at $ExePath"
}
Write-Host "Build succeeded: $ExePath" -ForegroundColor Green

if ($RunSelfTest) {
    Write-Host "[2/3] Running --selftest verification..." -ForegroundColor Cyan
    $proc = Start-Process -FilePath $ExePath -ArgumentList "--selftest" -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        throw "Self-test failed with exit code $($proc.ExitCode)"
    }
    Write-Host "Self-test passed (exit code 0)." -ForegroundColor Green
}

if ($RenderScreenshots) {
    Write-Host "[3/3] Rendering high-DPI verification screenshots..." -ForegroundColor Cyan
    $proc = Start-Process -FilePath $ExePath -ArgumentList "--render-screenshots" -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        throw "Screenshot rendering failed with exit code $($proc.ExitCode)"
    }
    Write-Host "Screenshots rendered successfully." -ForegroundColor Green
}
