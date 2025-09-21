param(
    [string]$ApiKey,
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$OutputDirectory,
    [switch]$SkipPush
)

$ErrorActionPreference = "Stop"

if (-not $ApiKey) {
    $ApiKey = $env:NUGET_API_KEY
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "NuGet API key not provided. Pass -ApiKey or set NUGET_API_KEY environment variable."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot "artifacts/nuget"
}

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$projects = @(
    @{ Path = "src/Cohere.Client/Cohere.Client.csproj"; Id = "Cohere.Client" },
    @{ Path = "src/Cohere.Extensions.AI/Cohere.Extensions.AI.csproj"; Id = "Cohere.Extensions.AI" }
)

$packages = @()

foreach ($project in $projects) {
    $projectPath = Resolve-Path (Join-Path $repoRoot $project.Path)
    Write-Host "Packing $($project.Id) from $projectPath" -ForegroundColor Cyan

    $packArgs = @(
        "pack",
        $projectPath,
        "-c", $Configuration,
        "-o", $OutputDirectory,
        "/p:IncludeSymbols=false"
    )

    if ($Version) {
        $packArgs += "/p:PackageVersion=$Version"
        $packArgs += "/p:Version=$Version"
    }

    & dotnet @packArgs

    $packageFile = Get-ChildItem -Path $OutputDirectory -Filter "$($project.Id).*nupkg" -File |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $packageFile) {
        throw "Could not find package for $($project.Id) in $OutputDirectory"
    }

    $packages += $packageFile
}

if ($SkipPush) {
    Write-Host "SkipPush specified, will not push packages." -ForegroundColor Yellow
    $packages | ForEach-Object { Write-Host "Package ready: $($_.FullName)" }
    return
}

foreach ($package in $packages) {
    Write-Host "Pushing $($package.FullName)" -ForegroundColor Green
    $pushArgs = @(
        "nuget", "push",
        $package.FullName,
        "--api-key", $ApiKey,
        "--source", $Source,
        "--skip-duplicate"
    )

    & dotnet @pushArgs
}
