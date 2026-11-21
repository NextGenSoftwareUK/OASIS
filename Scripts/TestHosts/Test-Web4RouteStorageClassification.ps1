[CmdletBinding()]
param([string]$ClassifierPath = (Join-Path $PSScriptRoot '../verify_web4_hyperdrive_route_manifest.ps1'))
$ErrorActionPreference = 'Stop'
$tokens = $null; $errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $ClassifierPath), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Route classifier source has syntax errors.' }
$functions = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -in @('Get-Disposition','Get-ModeAwareManagerMembers') }, $true))
if ($functions.Count -ne 2) { throw 'Expected shipping route disposition and manager-member functions.' }
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
foreach ($body in @('return Manager.CreateProfileRequestContextAsync(id);','return _manager.ApplyAvatarProfileAsync(id, mutation);')) {
    if ((Get-Disposition $body 'Profile' 'ExampleController' @() $declared) -ne 'DirectModeAwareManagerBoundary') { throw 'Declared manager alias was classified as local.' }
}
if ((Get-Disposition 'return Manager.Options();' 'Options' 'ExampleController') -ne 'LocalOrNonStorageEndpoint') { throw 'Undeclared member name acquired a storage classification.' }
Write-Host 'Shipping route classifier passed existing six cases plus typed manager alias extraction and positive/negative checks; no runtime calls.'
