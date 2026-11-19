#Requires -Version 7.0
param([Parameter(Mandatory)][ValidateSet('IPFS','SOLID')][string]$Provider,[switch]$KeySet)
$ErrorActionPreference='Stop'
# Called under the reusable runner's environment lock, before starting its service.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$folder=if($Provider -eq 'IPFS'){'IPFSCurrent'}else{'SolidCurrent'}
$state=Join-Path $root "LocalState/COSMIC/$folder"
$results=Join-Path $root "TestResults/$folder"
$fixture=if($KeySet){'cql-keyset'}else{'cql-interactive'}
$closed=Join-Path $state ($fixture+'-complete.json')
if(!(Test-Path -LiteralPath $closed)){throw 'A verified completed fixture is required for archiving.'}
if((Test-Path -LiteralPath (Join-Path $state ($fixture+'.pending.json'))) -or (Test-Path -LiteralPath (Join-Path $state ($fixture+'.pending.json.next')))){throw 'Pending fixture must be reconciled, never archived as completed.'}
$closure=Get-Content -LiteralPath $closed -Raw|ConvertFrom-Json
if($closure.phase -ne 'complete'){throw 'Fixture cleanup is not verified complete.'}
$ports=if($Provider -eq 'IPFS'){@(18547,18548,18549,5088)}else{@(13000,5088)}
if(Get-NetTCPConnection -State Listen -LocalPort $ports -ErrorAction SilentlyContinue){throw 'Stop owned listeners before archiving completed fixtures.'}
$id=(Get-FileHash -LiteralPath $closed).Hash.Substring(0,24).ToLowerInvariant()
$mode=if($KeySet){'keyset'}else{'interactive'}
$names=if($Provider -eq 'IPFS'){@(("ipfs-cql-$mode-native.trx"),("cql-$mode-native.log"))}else{@(("solid-cql-$mode-native.trx"),("solid-cql-$mode-lifecycle.json"),("cql-$mode-native.log"))}
$paths=@($closed)
if($Provider -eq 'IPFS'){$paths+=Join-Path $state ($fixture+'-dna.json')}
foreach($name in $names){$path=Join-Path $results $name;if(Test-Path -LiteralPath $path){$paths+=$path}}
$entries=@(foreach($path in $paths){
    $source=[IO.Path]::GetFullPath($path)
    if(!$source.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Archive source escaped this checkout.'}
    if((Get-Item -LiteralPath $source).LinkType){throw 'Archive source must not be a link.'}
    $archive=Join-Path (Split-Path $source) 'Archive'
    $target=Join-Path $archive ($id+'-'+[IO.Path]::GetFileName($source))
    if(Test-Path -LiteralPath $target){throw 'Preserve existing archive; never overwrite.'}
    [pscustomobject]@{source=$source;target=$target;sha256=(Get-FileHash -LiteralPath $source).Hash}
})
$manifest=Join-Path $state "Archive/$id-$mode-archive.json"
New-Item -ItemType Directory -Path (Split-Path $manifest) -Force|Out-Null
if(Test-Path -LiteralPath $manifest){throw 'Prior archive outcome must be reviewed.'}
@{phase='planned';entries=$entries}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $manifest
foreach($entry in $entries){
    New-Item -ItemType Directory -Path (Split-Path $entry.target) -Force|Out-Null
    Move-Item -LiteralPath $entry.source -Destination $entry.target
    if((Get-FileHash -LiteralPath $entry.target).Hash -ne $entry.sha256){throw 'Archive hash mismatch; preserve partial outcome.'}
}
@{phase='complete';entries=$entries}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $manifest
Write-Output "Archived $($entries.Count) exact completed fixture/evidence files; no data or credentials deleted."
