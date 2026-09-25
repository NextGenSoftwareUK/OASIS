[CmdletBinding()]
param(
    [Parameter(Mandatory)][PSCredential]$Credential,
    [string]$OnodeBaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$OfflineGrantPublicKey,
    [Guid]$QuestId
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'OASIS Omniverse/OGEngineClient/TestProjects/OGEngine.Client.Tests/OGEngine.Client.Tests.csproj'

function Find-PropertyValue($node, [string[]]$names) {
    if ($null -eq $node) { return $null }
    foreach ($name in $names) {
        $property = $node.PSObject.Properties[$name]
        if ($property -and $property.Value) { return $property.Value }
    }
    foreach ($property in $node.PSObject.Properties) {
        if ($property.Value -is [pscustomobject]) {
            $value = Find-PropertyValue $property.Value $names
            if ($value) { return $value }
        }
    }
    return $null
}

$authUri = $OnodeBaseUrl.TrimEnd('/') + '/api/avatar/authenticate'
$grantUri = $OnodeBaseUrl.TrimEnd('/') + '/api/hyperdrive/sync/offline-session-grant'
$networkCredential = $Credential.GetNetworkCredential()
$auth = Invoke-RestMethod -Method Post -Uri $authUri -ContentType 'application/json' -Body (
    @{ username = $networkCredential.UserName; password = $networkCredential.Password } | ConvertTo-Json)
$bearer = Find-PropertyValue $auth @('jwtToken', 'JwtToken', 'token', 'Token')
$avatarId = Find-PropertyValue $auth @('id', 'Id', 'avatarId', 'AvatarId')
if (-not $bearer -or -not $avatarId) { throw 'Authentication did not return the required bearer token and avatar id.' }

# Fail at the deployed contract boundary before requiring release-only test inputs.
$probe = Invoke-WebRequest -Method Post -Uri $grantUri -SkipHttpErrorCheck -Headers @{ Authorization = "Bearer $bearer" } `
    -ContentType 'application/json' -Body (@{
        deviceId = [Guid]::NewGuid(); requestedLifetimeMinutes = 60; requestedScopes = @('hyperdrive.sync')
    } | ConvertTo-Json)
if ([int]$probe.StatusCode -lt 200 -or [int]$probe.StatusCode -ge 300) {
    throw "Hosted ONODE offline-session-grant contract failed with HTTP $([int]$probe.StatusCode) at $grantUri."
}
if ([string]::IsNullOrWhiteSpace($OfflineGrantPublicKey)) {
    throw 'OfflineGrantPublicKey is required after the deployed grant endpoint is available.'
}
if ($QuestId -eq [Guid]::Empty) { throw 'QuestId must identify a real development quest used by the account.' }

$names = @('OASIS_RUN_LIVE_THREE_GAME_SYNC', 'OASIS_LIVE_ONODE_BASE_URL', 'OASIS_LIVE_BEARER_TOKEN',
    'OASIS_LIVE_OFFLINE_GRANT_PUBLIC_KEY', 'OASIS_LIVE_AVATAR_ID', 'OASIS_LIVE_QUEST_ID')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $env:OASIS_RUN_LIVE_THREE_GAME_SYNC = '1'
    $env:OASIS_LIVE_ONODE_BASE_URL = $OnodeBaseUrl
    $env:OASIS_LIVE_BEARER_TOKEN = $bearer
    $env:OASIS_LIVE_OFFLINE_GRANT_PUBLIC_KEY = $OfflineGrantPublicKey
    $env:OASIS_LIVE_AVATAR_ID = [string]$avatarId
    $env:OASIS_LIVE_QUEST_ID = $QuestId.ToString('D')
    dotnet test $project -c Release --filter 'FullyQualifiedName~LiveThreeGameOnodeTests' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw "Live three-game synchronization test failed with exit code $LASTEXITCODE." }
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $prior[$name]) }
    $bearer = $null
    $networkCredential = $null
    $Credential = $null
    $auth = $null
}
