[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$expectedNethereumVersion = '7.0.0'

$providerProjects = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Providers') -Recurse -Filter '*.csproj' -File
$versionErrors = [Collections.Generic.List[string]]::new()
foreach ($project in $providerProjects) {
    [xml]$document = Get-Content -LiteralPath $project.FullName -Raw
    foreach ($reference in @($document.Project.ItemGroup.PackageReference)) {
        if ($reference.Include -like 'Nethereum.*' -and $reference.Version -ne $expectedNethereumVersion) {
            $relativePath = [IO.Path]::GetRelativePath($repoRoot, $project.FullName)
            $versionErrors.Add("$relativePath references $($reference.Include) $($reference.Version); expected $expectedNethereumVersion.")
        }
    }
}

if ($versionErrors.Count -gt 0) {
    throw "The active Nethereum provider graph is inconsistent:`n$($versionErrors -join [Environment]::NewLine)"
}

# ONODE.Client targets net10.0, where System.Text.Json is part of the shared framework. Keeping an explicit
# package reference produces NU1510 throughout the STAR CLI graph and lets the framework and package graphs drift.
$onodeClientProject = Join-Path $repoRoot 'ONODE/ONODEManager/NextGenSoftware.OASIS.ONODE.Client/NextGenSoftware.OASIS.ONODE.Client.csproj'
[xml]$onodeClientDocument = Get-Content -LiteralPath $onodeClientProject -Raw
$redundantJsonReference = @($onodeClientDocument.Project.ItemGroup.PackageReference) |
    Where-Object { $_.Include -eq 'System.Text.Json' }
if ($redundantJsonReference.Count -gt 0) {
    throw 'ONODE.Client targets net10.0 and must use the framework System.Text.Json assembly rather than an explicit package reference.'
}

$auditProjects = @(
    'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj',
    'Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.csproj',
    'STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj'
)

foreach ($relativeProject in $auditProjects) {
    $project = Join-Path $repoRoot $relativeProject
    $json = & dotnet package list --project $project --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) {
        throw "Dependency vulnerability audit failed for '$relativeProject' with exit code $LASTEXITCODE."
    }

    $report = $json | ConvertFrom-Json -Depth 100
    $vulnerablePackages = @(
        $report.projects.frameworks.topLevelPackages
        $report.projects.frameworks.transitivePackages
    ) | Where-Object { $_ -and @($_.vulnerabilities).Count -gt 0 }

    if ($vulnerablePackages.Count -gt 0) {
        $details = $vulnerablePackages | ForEach-Object {
            "$($_.id) $($_.resolvedVersion): $((@($_.vulnerabilities).advisoryUrl) -join ', ')"
        }
        throw "Vulnerable packages were found for '$relativeProject':`n$($details -join [Environment]::NewLine)"
    }
}

Write-Host "Dependency security validation passed. Nethereum provider graph: $expectedNethereumVersion; audited roots: $($auditProjects.Count)."
