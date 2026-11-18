<# Read selected process/artifact evidence only; never calls an application or database endpoint. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(4,10)][int]$Service,
    [Parameter(Mandatory)][ValidateSet('dev','staging','production')][string]$Environment,
    [Parameter(Mandatory)][guid]$InstanceId,
    [Parameter(Mandatory)][guid]$DeploymentId,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedSource,
    [switch]$VerifyProfileScope,
    [switch]$VerifyProfileRegistry
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$config = Get-Content -LiteralPath (Join-Path $taskRoot 'Scripts/railway_release_gate.json') -Raw | ConvertFrom-Json
$serviceId = $config.services."$Service"
if (!$serviceId) { throw "Missing WEB$Service service mapping." }
$environmentId = @{
    dev='a31011a2-d047-4057-b5e8-57d52ebcfefe'
    staging='e0796a6e-8afe-4a38-8472-675ef572f7d7'
    production='86de55b2-51e3-4c83-ae6b-806d554349cc'
}[$Environment]
$dll = if ($Service -eq 4) { 'NextGenSoftware.OASIS.API.ONODE.WebAPI.dll' }
    elseif ($Service -eq 5) { 'NextGenSoftware.OASIS.STAR.WebAPI.dll' }
    else { "NextGenSoftware.OASIS.Web$Service.WebAPI.dll" }
$shell = @'
set -eu
count=0
for p in /proc/[0-9]*; do
    exe=$(readlink "$p/exe" 2>/dev/null) || continue
    case "$exe" in */dotnet) ;; *) continue ;; esac
    if ! tr "\000" "\n" < "$p/cmdline" | grep -Fqx "__API_DLL__"; then continue; fi
    count=$((count+1))
    printf "api_pid=%s\n" "${p##*/}"
    tr "\000" "\n" < "$p/environ" | grep -E "^(OASIS_PAUSE_USAGE_SETTLEMENT|RAILWAY_GIT_COMMIT_SHA|RAILWAY_DEPLOYMENT_ID)="
    if grep -Fq "/app/NextGenSoftware.OASIS.API.Core.dll" "$p/maps"; then
        printf "core_mapped=true\n"
    else
        printf "core_mapped=false\n"
    fi
done
test "$count" -eq 1
# Inspect the actual mapped assembly for the new worker control literal. No
# assembly contents, environment listing, DNA or credentials leave this pipe.
if tr -d "\000" < /app/NextGenSoftware.OASIS.API.Core.dll | grep -aFq "USAGE_SETTLEMENT_PAUSED"; then
    printf "core_control_marker=true\n"
else
    printf "core_control_marker=false\n"
fi
if [ "__VERIFY_PROFILE_SCOPE__" = "1" ]; then
    if tr -d "\000" < /app/NextGenSoftware.OASIS.API.Core.dll | grep -aFq "AVATAR_PROFILE_ROUTE_REQUIRED"; then
        printf "profile_scope_marker=true\n"
    else
        printf "profile_scope_marker=false\n"
    fi
    if tr -d "\000" < /app/NextGenSoftware.OASIS.API.Core.dll | grep -aFq "HYPERDRIVE_SCOPED_PROVIDER_SHUTDOWN_DENIED"; then
        printf "scoped_lifecycle_marker=true\n"
    else
        printf "scoped_lifecycle_marker=false\n"
    fi
fi
if [ "__VERIFY_PROFILE_REGISTRY__" = "1" ]; then
    if tr -d "\000" < /app/NextGenSoftware.OASIS.API.Core.dll | grep -aFq "AVATAR_PROFILE_AUTHORITY_CHANGED"; then
        printf "profile_registry_marker=true\n"
    else
        printf "profile_registry_marker=false\n"
    fi
fi
'@
$shell = $shell.Replace('__API_DLL__', $dll)
$shell = $shell.Replace('__VERIFY_PROFILE_SCOPE__', $(if ($VerifyProfileScope) { '1' } else { '0' }))
$shell = $shell.Replace('__VERIFY_PROFILE_REGISTRY__', $(if ($VerifyProfileRegistry) { '1' } else { '0' }))
# All substitutions are validated numeric IDs or fixed source-owned DLL names.
$remote = "sh -c '" + $shell + "'"
$rows = @(& railway ssh --project 712f50f6-3b7b-46d4-b229-44b49922f16b --service $serviceId --environment $environmentId --deployment-instance $InstanceId $remote 2>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0) { throw "Selected process inspection failed (exit $LASTEXITCODE); full SSH output is intentionally not emitted." }
$selected = @{}
foreach ($row in $rows) {
    if ($row -match '^(api_pid|OASIS_PAUSE_USAGE_SETTLEMENT|RAILWAY_GIT_COMMIT_SHA|RAILWAY_DEPLOYMENT_ID|core_mapped|core_control_marker|profile_scope_marker|scoped_lifecycle_marker|profile_registry_marker)=(.*)$') {
        if ($selected.ContainsKey($matches[1])) { throw 'More than one API process supplied evidence.' }
        $selected[$matches[1]] = $matches[2]
    }
}
$verified = $selected.OASIS_PAUSE_USAGE_SETTLEMENT -eq 'true' -and
    $selected.RAILWAY_GIT_COMMIT_SHA -eq $ExpectedSource -and
    $selected.RAILWAY_DEPLOYMENT_ID -eq $DeploymentId.ToString() -and
    $selected.core_mapped -eq 'true' -and $selected.core_control_marker -eq 'true'
if ($VerifyProfileScope) {
    $verified = $verified -and $selected.profile_scope_marker -eq 'true' -and $selected.scoped_lifecycle_marker -eq 'true'
}
if ($VerifyProfileRegistry) {
    $verified = $verified -and $selected.profile_registry_marker -eq 'true'
}
[pscustomobject]@{
    service="WEB$Service"; environment=$Environment; instance=$InstanceId.ToString()
    deployment=$selected.RAILWAY_DEPLOYMENT_ID; source=$selected.RAILWAY_GIT_COMMIT_SHA
    apiProcess=$selected.api_pid; settlementPause=$selected.OASIS_PAUSE_USAGE_SETTLEMENT
    coreMapped=$selected.core_mapped; coreControlMarker=$selected.core_control_marker
    profileScopeMarker=$selected.profile_scope_marker; scopedLifecycleMarker=$selected.scoped_lifecycle_marker
    profileRegistryMarker=$selected.profile_registry_marker
    verified=$verified
} | ConvertTo-Json -Compress
if (!$verified) { throw 'Running source, pause flag or loaded control assembly does not match acceptance.' }
