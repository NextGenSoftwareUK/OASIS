[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$tokens = $null
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Invoke-OASISGlobalRelease.ps1'), [ref]$tokens, [ref]$errors)
if ($errors) { throw $errors }
foreach ($name in @('Get-LatestGitHubReleaseVersion', 'Get-NextOptionalReleaseVersion', 'Get-NextPatchVersion')) {
    $function = $ast.Find({ param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    if (-not $function) { throw "Missing release function $name" }
    Invoke-Expression $function.Extent.Text
}

# Match Invoke-RestMethod's single pipeline object for a JSON array, rather
# than a mock that accidentally enumerates it and conceals the regression.
function Invoke-RestMethod {
    param($Uri, $Headers)
    if ($Uri -like '*/releases?*') {
        Write-Output -NoEnumerate @(
            [pscustomobject]@{ tag_name='v9.0.0'; draft=$true; prerelease=$false; published_at=$null },
            [pscustomobject]@{ tag_name='v8.0.0'; draft=$false; prerelease=$true; published_at=$null },
            [pscustomobject]@{ tag_name='v1.0.0'; draft=$false; prerelease=$false; published_at='2026-09-29T00:43:08Z' })
    } elseif ($Uri -like '*/commits?*') {
        Write-Output -NoEnumerate @([pscustomobject]@{
            sha=('a' * 40); commit=[pscustomobject]@{message='fix: correct release contract'} })
    } else { throw "Unexpected test request: $Uri" }
}
function Get-ReleaseLabelOverride { param($Repository, $Commits); return $null }
$Offline = $false
$VersionMode = 'Automatic'
$Operation = 'Plan'
$AllowBlockedPlan = $false
$versionDecisions = [Collections.Generic.List[object]]::new()
$baseline = Get-LatestGitHubReleaseVersion @('v') 'example/component'
if ($baseline -ne '1.0.0') { throw "Wrong baseline: $baseline" }
$next = Get-NextOptionalReleaseVersion 'example/component' @('v')
if ($next -ne '1.0.1') { throw "Wrong next version: $next" }
Write-Host 'GitHub release and commit array regression checks passed.'
