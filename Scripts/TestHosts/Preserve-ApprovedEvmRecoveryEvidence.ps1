#Requires -Version 7.0
[CmdletBinding()]
param([switch]$PreserveApprovedEvidence)
$ErrorActionPreference='Stop'
# User approved preserving the missing old node's evidence and recovering the
# SAME environment. This does not reconcile or re-prove old chain outcomes.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/EthereumCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    foreach($port in 18545,18546){if(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue){throw 'Old/native EVM endpoint is active; never replace it.'}}
    if(Get-CimInstance Win32_Process|Where-Object{$_.Name -eq 'node.exe' -and $_.CommandLine -match 'hardhat.*\bnode\b'}){throw 'An existing Hardhat process must be reviewed, not replaced.'}
    foreach($p in @('TestResults/Web3CoreCurrent/history-response-loss.pending.json','TestResults/EthereumCurrent/ethereum-accepted-loss.pending.json','TestResults/EthereumCurrent/delete-key-native.pending.json')){
        if(Test-Path -LiteralPath (Join-Path $root $p)){throw 'Unreviewed original chain intent exists; preserve without recovery.'}
    }
    $routing=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/bin/Debug/net10.0/TestResults/Web3CoreRouting/routing.db'
    foreach($suffix in '-wal','-shm'){if(Test-Path -LiteralPath ($routing+$suffix)){throw 'Routing database has a live/unreviewed sidecar.'}}
    if((Get-FileHash -LiteralPath $routing).Hash -ne '1A826BEFECEBFF8A6EC2628447FED96BE0575ABBC17A83E27C1817227CA69E96'){throw 'Original reviewed routing database changed.'}
    if(!(Test-Path -LiteralPath (Join-Path $work 'storage-key.dpapi'))){throw 'Retained storage key required; never silently replace it.'}
    $pending=Join-Path $work 'evm-recovery-preservation.pending.json'
    $current=Join-Path $work 'evm-recovery-preservation.current.json'
    if((Test-Path -LiteralPath $pending) -or (Test-Path -LiteralPath $current)){throw 'Review original preservation outcome; do not repeat.'}
    function RequireOrdinaryPath([string]$Path){
        $item=Get-Item -LiteralPath $Path -Force
        if(!$item.FullName.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Preservation path is outside this checkout.'}
        $walk=$item
        while($walk -and $walk.FullName -ne $root){
            if($walk.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Preservation cannot move through a reparse point.'}
            $walk=if($walk.PSIsContainer){$walk.Parent}else{$walk.Directory}
        }
        return $item
    }
    $sources=@($routing)
    foreach($name in @('reject-second-holon.json','LegacyStorageProtocolFixture.json','UnsupportedStorageProtocolFixture.json','OwnedErc20Fixture.json','Protocol3WithoutCheckedStorageFixture.json','token-escrow.json','default-storage.json','ethereum-native-current.trx','evm-current.log','evm-current.err')){$sources+=Join-Path $work $name}
    foreach($name in @('web3-storage.json','web3-nft-boundary.json','web3-nft-noop.json','web3-native-current.trx')){$sources+=Join-Path $root ('TestResults/Web3CoreCurrent/'+$name)}
    $items=@()
    foreach($source in $sources){
        if(!(Test-Path -LiteralPath $source)){continue}
        $file=RequireOrdinaryPath $source
        $hash=(Get-FileHash -LiteralPath $file.FullName).Hash
        $target=if($source -eq $routing){Join-Path $work ('routing-unverified-'+$hash+'.db')}else{$file.FullName+'.pre-approved-recovery-'+$hash}
        [void](RequireOrdinaryPath (Split-Path $target))
        if(Test-Path -LiteralPath $target){throw 'Preserved target exists; never overwrite original evidence.'}
        $items+=[ordered]@{Source=$file.FullName;Preserved=$target;Bytes=$file.Length;SHA256=$hash}
    }
    if(!$PreserveApprovedEvidence){[pscustomobject]@{Files=$items.Count;Bytes=($items|ForEach-Object{$_.Bytes}|Measure-Object -Sum).Sum;RoutingSHA256=$items[0].SHA256;OldChainOutcomes='unverified';NativeStarts=0}|ConvertTo-Json -Compress;return}
    $proof=[ordered]@{State='before-preservation';OldChainOutcomes='unverified-node-unavailable';NewEnvironment=$false;Items=$items}
    $bytes=[Text.Encoding]::UTF8.GetBytes(($proof|ConvertTo-Json -Depth 5 -Compress))
    $file=[IO.File]::Open($pending,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$file.Write($bytes);$file.Flush($true)}finally{$file.Dispose()}
    foreach($item in $items){
        if((Get-FileHash -LiteralPath $item.Source).Hash -ne $item.SHA256){throw 'Original evidence changed before move.'}
        Move-Item -LiteralPath $item.Source -Destination $item.Preserved -ErrorAction Stop
        if((Get-FileHash -LiteralPath $item.Preserved).Hash -ne $item.SHA256){throw 'Preserved evidence hash differs; stop without recovery.'}
    }
    Move-Item -LiteralPath $pending -Destination $current -ErrorAction Stop
    [pscustomobject]@{Files=$items.Count;Bytes=($items|ForEach-Object{$_.Bytes}|Measure-Object -Sum).Sum;ProofSHA256=(Get-FileHash -LiteralPath $current).Hash;OldChainOutcomes='still-unverified';NativeStarts=0}|ConvertTo-Json -Compress
}finally{$lease.Dispose()}
