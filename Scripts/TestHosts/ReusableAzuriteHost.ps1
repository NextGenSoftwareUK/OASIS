function Start-ReusableAzuriteHost {
    param([Parameter(Mandatory)][string]$Workspace,[string]$DataDirectory)
    $retainedRuntime=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/dist/src/blob/main.js'
    $retainedPackage=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/package.json'
    if(!(Test-Path -LiteralPath $retainedRuntime) -or (Get-Content $retainedPackage -Raw|ConvertFrom-Json).version -ne '3.37.0'){throw 'Pinned retained Azurite missing; no download or substitute.'}
    if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue){throw 'Azurite port already occupied.'}
    $retainedStart=[Diagnostics.ProcessStartInfo]::new((Get-Command node.exe).Source)
    $retainedStart.UseShellExecute=$false;$retainedStart.CreateNoWindow=$true;$retainedStart.WindowStyle='Hidden'
    $retainedStart.RedirectStandardInput=$true;$retainedStart.RedirectStandardOutput=$true;$retainedStart.RedirectStandardError=$true
    $retainedStart.WorkingDirectory=$Workspace
    if(!$DataDirectory){$DataDirectory=Join-Path $Workspace 'conformance-data'}
    foreach($retainedArgument in @((Join-Path $PSScriptRoot 'RetainedAzuriteHost.cjs'),$retainedRuntime,$DataDirectory)){[void]$retainedStart.ArgumentList.Add($retainedArgument)}
    $retainedProcess=[Diagnostics.Process]::Start($retainedStart)
    [pscustomobject]@{Process=$retainedProcess;Output=$retainedProcess.StandardOutput.ReadToEndAsync();Error=$retainedProcess.StandardError.ReadToEndAsync();Workspace=$Workspace;Runtime=$retainedRuntime}
}

function Assert-ReusableAzuriteListener {
    param([Parameter(Mandatory)]$HostHandle)
    if($HostHandle.Process.HasExited){throw 'Owned retained host exited before readiness.'}
    $retainedListener=Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue
    if(!$retainedListener){return $false}
    $retainedChild=Get-CimInstance Win32_Process -Filter ('ProcessId='+$retainedListener.OwningProcess)
    if($retainedListener.LocalAddress -ne '127.0.0.1' -or $retainedChild.ParentProcessId -ne $HostHandle.Process.Id -or $retainedChild.CommandLine -notlike ('*'+$HostHandle.Runtime+'*')){throw 'Listener does not belong to the owned retained Azurite child.'}
    return $true
}

function Stop-ReusableAzuriteHost {
    param([Parameter(Mandatory)]$HostHandle)
    if(!$HostHandle.Process.HasExited){
        $HostHandle.Process.StandardInput.WriteLine('shutdown');$HostHandle.Process.StandardInput.Flush();$HostHandle.Process.StandardInput.Close()
        if(!$HostHandle.Process.WaitForExit(15000)){throw 'Graceful Azurite shutdown did not complete; preserve the runtime and store. No forced stop.'}
    }
    [IO.File]::WriteAllText((Join-Path $HostHandle.Workspace 'azurite.out.log'),$HostHandle.Output.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $HostHandle.Workspace 'azurite.err.log'),$HostHandle.Error.GetAwaiter().GetResult())
    if($HostHandle.Process.ExitCode -ne 0){throw 'Retained Azurite host exited unsuccessfully; inspect bounded logs.'}
    if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue){throw 'Azurite listener remains after graceful shutdown.'}
    $HostHandle.Process.Dispose()
}
