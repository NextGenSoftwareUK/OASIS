$ErrorActionPreference = 'Stop'
$tokens=$null; $errors=$null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Invoke-OASISGlobalRelease.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Planner syntax is invalid.' }
foreach ($name in @('Get-PlannedPackageVersion','Get-NextPatchVersion')) {
    $function = $ast.Find({param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name}, $true)
    . ([scriptblock]::Create($function.Extent.Text))
}
function Get-SourceVersion($Project) { return $testSourceVersion }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [pscustomobject]@{PackageId='Test.Package';File=[pscustomobject]@{FullName=(Join-Path $repoRoot 'Test/Test.csproj')}}
$published = [pscustomobject]@{Version='2.3.4';Repository=[pscustomobject]@{commit=('a'*40);url='https://github.com/NextGenSoftwareUK/OASIS'}}
$Offline=$false; $testSourceVersion='1.0.0'
$sourceDecisions=[Collections.Generic.List[object]]::new()
$versionDecisions=[Collections.Generic.List[object]]::new()
$sourceGraphs=[pscustomobject]@{'Test.Package'=[pscustomobject]@{changed=$false}}
$VersionMode='Automatic'
if ($null -ne (Get-PlannedPackageVersion $project $published)) { throw 'Unchanged package received a version.' }
$sourceGraphs.'Test.Package'.changed=$true
foreach ($case in @(@('Automatic','2.3.5'),@('Patch','2.3.5'),@('Minor','2.4.0'),@('Major','3.0.0'))) {
    $VersionMode=$case[0]
    if ((Get-PlannedPackageVersion $project $published) -ne $case[1]) { throw "Wrong $VersionMode version." }
}
$testSourceVersion='2.3.8'; $VersionMode='Automatic'
if ((Get-PlannedPackageVersion $project $published) -ne '2.3.8') { throw 'Advanced source version was unnecessarily bumped.' }
$VersionMode='Manual'; $ManualVersion='2.3.9'
if ((Get-PlannedPackageVersion $project $published) -ne '2.3.9') { throw 'Manual version ignored.' }
$ManualVersion='2.3.4'; $rejected=$false
try { Get-PlannedPackageVersion $project $published | Out-Null } catch { $rejected=$true }
if (-not $rejected) { throw 'Immutable manual identity accepted.' }
$sourceGraphs.'Test.Package'.changed=$false
if ($null -ne (Get-PlannedPackageVersion $project $published)) { throw 'Manual mode republished unchanged source.' }
Write-Host 'NuGet version selection invariants: PASS (unchanged, patch/minor/major, advanced source, manual, immutable rejection).'
