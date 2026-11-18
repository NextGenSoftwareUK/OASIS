<# Apply only the reviewed dev watch filter; default mode inspects without writing. #>
[CmdletBinding()]
param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'railway_dev_watch_policy.json') -Raw | ConvertFrom-Json
& python -B (Join-Path $PSScriptRoot 'validate_railway_dev_watch_policy.py')
if ($LASTEXITCODE -ne 0) { throw 'The shipping-input guard rejected the watch policy.' }

Push-Location $repoRoot
try {
    $projectJson = & railway status --json
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the existing Railway project.' }
    $project = $projectJson | ConvertFrom-Json
} finally { Pop-Location }
if ($project.id -ne $policy.projectId) { throw 'The linked Railway project does not match the policy.' }
$environment = @($project.environments.edges.node | Where-Object { $_.name -eq 'dev' })
if ($environment.Count -ne 1 -or $environment[0].id -ne $policy.environmentId) {
    throw 'The policy does not target the unique dev environment.'
}

$auth = Get-Content -LiteralPath (Join-Path $env:USERPROFILE '.railway/config.json') -Raw | ConvertFrom-Json
if (!$auth.user.token) { throw 'The existing Railway CLI user authentication is unavailable.' }
$headers = @{ Authorization = ('Bearer ' + $auth.user.token) }
function Invoke-WatchApi([string]$Query, [hashtable]$Variables) {
    $parameters = @{
        Uri = 'https://backboard.railway.com/graphql/v2'
        Method = 'Post'
        Headers = $headers
        ContentType = 'application/json'
        UserAgent = 'railway-cli'
        TimeoutSec = 30
        Body = (@{ query = $Query; variables = $Variables } | ConvertTo-Json -Depth 6 -Compress)
    }
    $response = Invoke-RestMethod @parameters
    if ($response.errors) { throw 'Railway rejected the scoped watch-policy operation.' }
    return $response.data
}
function Read-WatchInstance([string]$ServiceId) {
    $variables = @{ service = $ServiceId; environment = $policy.environmentId }
    $data = Invoke-WatchApi 'query($service:String!,$environment:String!){serviceInstance(serviceId:$service,environmentId:$environment){serviceId environmentId watchPatterns dockerfilePath buildCommand source{repo image} latestDeployment{id}}}' $variables
    $instance = $data.serviceInstance
    if (!$instance -or $instance.serviceId -ne $ServiceId -or $instance.environmentId -ne $policy.environmentId) {
        throw 'Watch-policy readback returned a different service or environment.'
    }
    return $instance
}
function Same-Patterns($Actual) {
    return ((@($Actual) | ConvertTo-Json -Compress) -eq (@($policy.watchPatterns) | ConvertTo-Json -Compress))
}
function Assert-ShippingInstance($Instance, [int]$Api) {
    if ($Instance.source.repo -ne $policy.repository -or $Instance.source.image -or
        $Instance.dockerfilePath -ne "Docker/Dockerfile.web$Api" -or $Instance.buildCommand) {
        throw "WEB$Api uses a different source/build contract; review before changing its watch filter."
    }
}

# Preflight the entire inventory before any write; never replace an intentional filter.
$targets = foreach ($number in $policy.apis) {
    $serviceMatches = @($project.services.edges.node | Where-Object { $_.name -match "^WEB${number}(\s|$)" })
    if ($serviceMatches.Count -ne 1) { throw "Expected exactly one WEB$number service; no settings changed." }
    $service = $serviceMatches[0]
    $before = Read-WatchInstance $service.id
    Assert-ShippingInstance $before $number
    if (@($before.watchPatterns).Count -ne 0 -and !(Same-Patterns $before.watchPatterns)) {
        throw "WEB$number has an intentional watch filter requiring review; no settings changed."
    }
    [pscustomobject]@{ Api = $number; Id = $service.id; Name = $service.name; Before = $before }
}
foreach ($target in $targets) {
    $before = Read-WatchInstance $target.Id
    Assert-ShippingInstance $before $target.Api
    if ((@($before.watchPatterns) | ConvertTo-Json -Compress) -ne (@($target.Before.watchPatterns) | ConvertTo-Json -Compress)) {
        throw "WEB$($target.Api) watch settings changed after preflight; stopped without overwriting them."
    }
    $saved = Same-Patterns $before.watchPatterns
    if ($Apply -and !$saved) {
        $variables = @{ service = $target.Id; environment = $policy.environmentId; input = @{ watchPatterns = @($policy.watchPatterns) } }
        $result = Invoke-WatchApi 'mutation($service:String!,$environment:String!,$input:ServiceInstanceUpdateInput!){serviceInstanceUpdate(serviceId:$service,environmentId:$environment,input:$input)}' $variables
        if ($result.serviceInstanceUpdate -ne $true) { throw "WEB$($target.Api) watch settings were not acknowledged." }
        $after = Read-WatchInstance $target.Id
        Assert-ShippingInstance $after $target.Api
        if (!(Same-Patterns $after.watchPatterns)) { throw "WEB$($target.Api) saved watch settings failed readback." }
        if ($after.latestDeployment.id -ne $before.latestDeployment.id) {
            throw "WEB$($target.Api) deployment identity changed during the operation; stop and inspect before continuing."
        }
        $saved = $true
    }
    [pscustomobject]@{
        service = $target.Name
        environment = 'dev'
        applied = [bool]$Apply
        verifiedSaved = [bool]$saved
        before = @($before.watchPatterns)
        expected = @($policy.watchPatterns)
        deploymentId = $before.latestDeployment.id
    } | ConvertTo-Json -Depth 4 -Compress
}
