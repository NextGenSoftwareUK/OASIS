[CmdletBinding()]
param([string]$ClassifierPath = (Join-Path $PSScriptRoot '../verify_web4_hyperdrive_route_manifest.ps1'))
$ErrorActionPreference = 'Stop'
$tokens = $null; $errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $ClassifierPath), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Route classifier source has syntax errors.' }
$functions = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -in @('Get-Disposition','Get-ModeAwareManagerMembers','Get-InjectedStorageMembers') }, $true))
if ($functions.Count -ne 3) { throw 'Expected shipping route disposition and manager-member functions.' }
foreach ($function in $functions) { . ([scriptblock]::Create($function.Extent.Text)) }
$cases = @(
    @{Body='return _repository.SaveNotificationSettingsAsync();'; Method='SetMyNotifications'; Controller='SubscriptionOwnerController'; Members=@(); Expected='InjectedServiceRepositoryOrClient'},
    @{Body='public Task Grid([FromServices] OLandCommerceService commerce) => commerce.Grid();'; Method='Grid'; Controller='OLandController'; Members=@(); Expected='InjectedServiceRepositoryOrClient'},
    @{Body='return _subscriptions.GetUsageSummaryAsync();'; Method='Usage'; Controller='SubscriptionController'; Members=@('_subscriptions'); Expected='InjectedServiceRepositoryOrClient'},
    @{Body='return CqlV1Endpoint.ExecuteScalarAsync();'; Method='Scalar'; Controller='CqlV1Controller'; Members=@(); Expected='InjectedServiceRepositoryOrClient'},
    @{Body='return Data.LoadHolonAsync(id);'; Method='Get'; Controller='MapRoutesController'; Members=@(); Expected='DirectModeAwareManagerBoundary'},
    @{Body='return Ok(new { enabled = true });'; Method='Options'; Controller='ExampleController'; Members=@('_subscriptions'); Expected='LocalOrNonStorageEndpoint'}
)
foreach ($case in $cases) {
    $actual = Get-Disposition -body $case.Body -methodName $case.Method -controllerName $case.Controller -injectedMembers $case.Members
    if ($actual -ne $case.Expected) { throw "Wrong storage classification for $($case.Controller).$($case.Method): $actual" }
}
$declared = @(Get-ModeAwareManagerMembers "private readonly AvatarManager _manager;`nprivate AvatarManager Manager => _manager;`nprivate string Label => value;")
if ($declared.Count -ne 2 -or 'Manager' -notin $declared -or '_manager' -notin $declared) { throw 'Typed manager declaration extraction failed.' }
if ((Get-Disposition 'return Manager.CreateProfileRequestContextAsync(id);' 'Profile' 'ExampleController' @() $declared) -ne 'DirectModeAwareManagerBoundary') { throw 'Declared manager property was classified as local.' }
if ((Get-Disposition 'return _manager.ApplyAvatarProfileAsync(id, mutation);' 'Profile' 'ExampleController' @() $declared) -ne 'CoveredFeatureManagerDelegation') { throw 'Existing manager-field classification changed.' }
if ((Get-Disposition 'return Manager.Options();' 'Options' 'ExampleController') -ne 'LocalOrNonStorageEndpoint') { throw 'Undeclared member name acquired a storage classification.' }
Write-Host 'Shipping route classifier passed existing six cases plus typed manager alias extraction and positive/negative checks; no runtime calls.'

$primary = @(Get-InjectedStorageMembers 'public sealed class ExampleController(MongoSubscriptionUsageRepository repository, IConfiguration configuration, IPaidOverageGateway gateway = null) : ControllerBase { }')
if ($primary.Count -ne 1 -or $primary[0] -ne 'repository') { throw 'Primary constructor repository extraction failed.' }
if ((Get-Disposition 'return repository.GetOwnedAppsAsync(actor);' 'Apps' 'ExampleController' $primary) -ne 'InjectedServiceRepositoryOrClient') { throw 'Primary constructor repository was classified as local.' }
if ((Get-Disposition 'return repository.GetOwnedAppsAsync(actor);' 'Apps' 'ExampleController') -ne 'LocalOrNonStorageEndpoint') { throw 'Undeclared repository acquired storage classification.' }

$typedReads = @(Get-InjectedStorageMembers "private readonly AccountClosurePrerequisiteReader _reader;`nprivate readonly ISubscriptionCheckoutJournal _checkoutJournal;")
if ($typedReads.Count -ne 2) { throw 'Typed storage reader/journal extraction failed.' }
foreach ($member in $typedReads) {
    if ((Get-Disposition ("return " + $member + ".ReadAsync();") 'Read' 'ExampleController' $typedReads) -ne 'InjectedServiceRepositoryOrClient') { throw 'Storage reader/journal was classified as local.' }
}
if ((Get-Disposition 'return await ReadCheckoutSessionStatusAsync(actor, id);' 'GetCheckoutSessionStatus' 'ExampleController') -ne 'ControllerActionDelegation') { throw 'Awaited controller helper delegation was classified as local.' }
if ((Get-Disposition 'return await Task.FromResult(Ok());' 'Get' 'ExampleController') -ne 'LocalOrNonStorageEndpoint') { throw 'Qualified local async work acquired a controller delegation classification.' }
Write-Host 'Storage reader/journal and awaited unqualified helper classifications passed; no runtime calls.'

if ((Get-Disposition 'return await ReadAsync(AvatarManager.Instance);' 'Get' 'ExampleController') -ne 'DirectModeAwareManagerBoundary') { throw 'Helper delegation masked the direct storage boundary.' }
