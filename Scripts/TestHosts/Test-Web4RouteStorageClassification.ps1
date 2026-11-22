[CmdletBinding()]
param([string]$ClassifierPath = (Join-Path $PSScriptRoot '../verify_web4_hyperdrive_route_manifest.ps1'))
$ErrorActionPreference = 'Stop'
$tokens = $null; $errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $ClassifierPath), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Route classifier source has syntax errors.' }
$functions = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-Disposition' }, $true))
if ($functions.Count -ne 1) { throw 'Expected exactly one shipping route disposition function.' }
. ([scriptblock]::Create($functions[0].Extent.Text))
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
Write-Host 'Shipping route classifier passed six focused static cases; no provider/API response mocks or runtime calls.'
