#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$work=Join-Path $taskRoot 'TestResults/HyperDriveTron'
$tool=(Get-Command javap -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$lease=[IO.File]::Open((Join-Path $work 'runtime-inspection.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$temporaryDirectory=Join-Path $work 'version-inspection'
$classPath=Join-Path $temporaryDirectory 'Version.class'
$madeClass=$false;$process=$null
$failures=[Collections.Generic.List[Exception]]::new();$json=$null
try{
    $fences=@{'pending-native-outcome.json'='9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE';'independent-prepared-native-pending.json'='2494A7BBEA823D2E0BE11A2869EDA891A23E1FD64ABABE975F75DDAC5A743070'}
    foreach($name in $fences.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash-ne $fences[$name]){throw 'Protected TRON original changed; inspect first.'}}
    if(Test-Path -LiteralPath $classPath){throw 'Existing class inspection file requires review; never overwrite it.'}
    if(Test-Path -LiteralPath $temporaryDirectory){
        $directory=Get-Item -LiteralPath $temporaryDirectory
        if($directory.Attributes-band [IO.FileAttributes]::ReparsePoint){throw 'Inspection directory must not be a reparse point.'}
    }else{New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null}
    $raw=& wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/inspect-retained-tron-jar.sh
    if($LASTEXITCODE-ne 0){throw 'Offline retained JAR inspection failed; no runtime replaced.'}
    $metadata=($raw-join "`n")|ConvertFrom-Json
    $bytes=[Convert]::FromBase64String($metadata.versionClassBase64)
    if($bytes.Length-lt 1-or $bytes.Length-gt 65536-or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()-ne $metadata.versionClassSha256){throw 'Bounded original class digest required.'}
    $metadata.PSObject.Properties.Remove('versionClassBase64')
    $metadata.PSObject.Properties.Remove('extractedFiles')
    # One tiny generated diagnostic input, never a JAR/runtime copy. No class execution.
    $classFile=[IO.File]::Open($classPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    $madeClass=$true
    try{$classFile.Write($bytes)}finally{$classFile.Dispose()}
    $start=[Diagnostics.ProcessStartInfo]::new($tool)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($argument in @('-J-Xmx64m','-J-XX:MaxMetaspaceSize=64m','-J-XX:-UsePerfData','-constants','-private',$classPath)){$start.ArgumentList.Add($argument)}
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    if(!$process.WaitForExit(30000)){$process.Kill();$process.WaitForExit();throw 'Owned read-only javap exceeded its30second budget.'}
    $text=$stdout.GetAwaiter().GetResult();$errors=$stderr.GetAwaiter().GetResult()
    if($process.ExitCode-ne 0-or $text.Length-gt 65536){throw ('Retained JDK class inspection failed: '+$errors)}
    foreach($name in $fences.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash-ne $fences[$name]){throw 'Protected TRON original changed during offline inspection.'}}
    $metadata|Add-Member -NotePropertyName javap -NotePropertyValue $text
    $metadata|Add-Member -NotePropertyName inspectionTool -NotePropertyValue $tool
    $metadata|Add-Member -NotePropertyName temporaryClassBytes -NotePropertyValue $bytes.Length
    $metadata|Add-Member -NotePropertyName retainedTemporaryClassFiles -NotePropertyValue 0
    $json=$metadata|ConvertTo-Json -Depth 8
}catch{$failures.Add($_.Exception)}finally{
    try{
        if($process){$process.Dispose()}
        if($madeClass){
            if((Get-FileHash -LiteralPath $classPath).Hash.ToLowerInvariant()-ne $metadata.versionClassSha256){throw 'Owned class changed; preserve it for inspection.'}
            Remove-Item -LiteralPath $classPath
        }
    }catch{$failures.Add($_.Exception)}
    try{
        if($failures.Count-eq 0){[IO.File]::WriteAllText((Join-Path $work 'jar-inspection-current.json'),$json)}
    }catch{$failures.Add($_.Exception)}finally{$lease.Dispose()}
}
if($failures.Count-eq 1){[Runtime.ExceptionServices.ExceptionDispatchInfo]::Capture($failures[0]).Throw()}
if($failures.Count-gt 1){throw [AggregateException]::new('Offline inspection/cleanup failed; original error first.',$failures)}
Write-Output $json
