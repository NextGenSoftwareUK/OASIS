#Requires -Version 7.0
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runner=Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1'
$lockPath=Join-Path $root 'TestResults/SubscriptionUsageCurrent/environment.lock'
$names=@('OASIS_OWNED_RECOVERY_REPLICA_URI','SUBSCRIPTION_TEST_MONGODB_URI')
$prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
try{
    foreach($present in @($false,$true)){
        foreach($name in $names){
            if($present){[Environment]::SetEnvironmentVariable($name,"prior-$name")}
            else{Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue}
        }
        $expected=@{};foreach($name in $names){$expected[$name]=[Environment]::GetEnvironmentVariable($name)}
        $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,27029)
        $listener.Start()
        try{
            $refused=$false
            try{& $runner}catch{if(!$_.Exception.Message.Contains('Owned port already in use')){throw};$refused=$true}
            if(!$refused -or !$listener.Server.IsBound){throw 'Occupied-port refusal must preserve unrelated listener.'}
            foreach($name in $names){if([Environment]::GetEnvironmentVariable($name) -cne $expected[$name]){throw "Environment restoration mismatch: $name"}}
            $reopened=[IO.File]::Open($lockPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$reopened.Dispose()
            Write-Output "PASS: occupied-port refusal, listener retained, lease released; existing variables=$present"
        }finally{$listener.Stop()}
    }
    $held=[IO.File]::Open($lockPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try{
        $refused=$false
        try{& $runner}catch [IO.IOException]{$refused=$true}
        if(!$refused -or !$held.CanRead){throw 'Concurrent runner must not acquire or close another owner lease.'}
        Write-Output 'PASS: concurrent lease refused; existing owner lease retained'
    }finally{$held.Dispose()}
}finally{
    foreach($name in $names){
        if($null -eq $prior[$name]){Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue}
        else{[Environment]::SetEnvironmentVariable($name,$prior[$name])}
    }
}
