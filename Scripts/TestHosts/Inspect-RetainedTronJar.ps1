#Requires -Version 7.0
param([switch]$FeeRules,[switch]$VMFeeRules)
$ErrorActionPreference='Stop'
if($FeeRules -and $VMFeeRules){throw 'Select one bounded inspection mode.'}
$taskRoot=[IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$work=Join-Path $taskRoot 'TestResults/HyperDriveTron'
$tool=(Get-Command javap -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$lease=[IO.File]::Open((Join-Path $work 'runtime-inspection.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$temporaryDirectory=Join-Path $work 'version-inspection'
$classPath=Join-Path $temporaryDirectory 'Version.class'
$madeClass=$false;$process=$null
$activeHash=$null
$failures=[Collections.Generic.List[Exception]]::new();$json=$null
try{
    $fences=@{'pending-native-outcome.json'='9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE';'independent-prepared-native-pending.json'='2494A7BBEA823D2E0BE11A2869EDA891A23E1FD64ABABE975F75DDAC5A743070'}
    foreach($name in $fences.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash-ne $fences[$name]){throw 'Protected TRON original changed; inspect first.'}}
    if(Test-Path -LiteralPath $classPath){throw 'Existing class inspection file requires review; never overwrite it.'}
    if(Test-Path -LiteralPath $temporaryDirectory){
        $directory=Get-Item -LiteralPath $temporaryDirectory
        if($directory.Attributes-band [IO.FileAttributes]::ReparsePoint){throw 'Inspection directory must not be a reparse point.'}
    }else{New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null}
    [string[]]$inspectionArguments=@()
    if($FeeRules){$inspectionArguments=@('--fee-rules')}
    if($VMFeeRules){$inspectionArguments=@('--vm-fee-rules')}
    $raw=& wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/inspect-retained-tron-jar.sh @inspectionArguments
    if($LASTEXITCODE-ne 0){throw 'Offline retained JAR inspection failed; no runtime replaced.'}
    $metadata=($raw-join "`n")|ConvertFrom-Json
    $materials=@([pscustomobject]@{entry='org/tron/program/Version.class';sha256=$metadata.versionClassSha256;base64=$metadata.versionClassBase64})
    if($FeeRules -or $VMFeeRules){$materials+=@($metadata.feeClassMaterials);$metadata.PSObject.Properties.Remove('feeClassMaterials')}
    $metadata.PSObject.Properties.Remove('versionClassBase64')
    $metadata.PSObject.Properties.Remove('extractedFiles')
    $inspections=[Collections.Generic.List[object]]::new();$totalBytes=0
    foreach($material in $materials){
    $bytes=[Convert]::FromBase64String($material.base64);$activeHash=$material.sha256
    if($bytes.Length-lt 1-or $bytes.Length-gt 262144-or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()-ne $activeHash){throw 'Bounded original class digest required.'}
    $totalBytes+=$bytes.Length
    # One generated diagnostic input at a time, never a JAR/runtime copy. No class execution.
    $classFile=[IO.File]::Open($classPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    $madeClass=$true
    try{$classFile.Write($bytes)}finally{$classFile.Dispose()}
    $start=[Diagnostics.ProcessStartInfo]::new($tool)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($argument in @('-J-Xmx64m','-J-XX:MaxMetaspaceSize=64m','-J-XX:-UsePerfData','-constants','-private')){$start.ArgumentList.Add($argument)}
    if($material.entry-ne 'org/tron/program/Version.class'){$start.ArgumentList.Add('-c')}
    $start.ArgumentList.Add($classPath)
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    if(!$process.WaitForExit(30000)){$process.Kill();$process.WaitForExit();throw 'Owned read-only javap exceeded its30second budget.'}
    $text=$stdout.GetAwaiter().GetResult();$errors=$stderr.GetAwaiter().GetResult()
    if($process.ExitCode-ne 0-or $text.Length-gt 2MB){throw ('Retained JDK class inspection failed: '+$errors)}
    if($material.entry-eq 'org/tron/program/Version.class'){$versionText=$text}else{
        $selected=[Collections.Generic.List[string]]::new();$include=$false;$methodCount=0
        foreach($line in ($text-split '\r?\n')){
            if($line-match '^  static \{\};'){$include=$false}
            elseif($line-match '^  \S.*\('){
                $include=$line-match '\b(consume|consumeMultiSignFee|consumeMemoFee|consumeBandwidth|useTransactionFee|consumeForCreateNewAccount|consumeBandwidthForCreateNewAccount|consumeFee\w*|contractCreateNewAccount|execute|calcFee|validateCommon|processBlock|processTransaction)\('
                if($VMFeeRules){$include=$line-match '\b\w*(Energy|energy|pay|Pay|exec|Exec|init|Init|finalization|Finalization|setResult|validate|create|call)\w*\('}
                if($include){$methodCount++}
            }
            if($include){$selected.Add($line)}
        }
        if($methodCount-eq 0){throw 'Expected fee methods absent; inspect manually instead of guessing.'}
        $inspections.Add([pscustomobject]@{entry=$material.entry;sha256=$activeHash;classBytes=$bytes.Length;selectedMethodCount=$methodCount;bytecode=($selected-join "`n")})
    }
    $process.Dispose();$process=$null
    if((Get-FileHash -LiteralPath $classPath).Hash.ToLowerInvariant()-ne $activeHash){throw 'Generated class changed; preserve for review.'}
    Remove-Item -LiteralPath $classPath;$madeClass=$false
    }
    foreach($name in $fences.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash-ne $fences[$name]){throw 'Protected TRON original changed during offline inspection.'}}
    $metadata|Add-Member -NotePropertyName javap -NotePropertyValue $versionText
    if($FeeRules -or $VMFeeRules){$metadata|Add-Member -NotePropertyName feeRuleInspections -NotePropertyValue @($inspections.ToArray())}
    $metadata|Add-Member -NotePropertyName inspectionTool -NotePropertyValue $tool
    $metadata|Add-Member -NotePropertyName temporaryClassBytes -NotePropertyValue $totalBytes
    $metadata|Add-Member -NotePropertyName retainedTemporaryClassFiles -NotePropertyValue 0
    $json=$metadata|ConvertTo-Json -Depth 8
    if($json.Length-gt 256KB){throw 'Compact fee inspection exceeds reviewed evidence bound; manual review required.'}
}catch{$failures.Add($_.Exception)}finally{
    try{
        if($process){$process.Dispose()}
        if($madeClass){
            if((Get-FileHash -LiteralPath $classPath).Hash.ToLowerInvariant()-ne $activeHash){throw 'Owned class changed; preserve it for inspection.'}
            Remove-Item -LiteralPath $classPath
        }
    }catch{$failures.Add($_.Exception)}
    try{
        if($failures.Count-eq 0){$report=if($VMFeeRules){'vm-fee-bytecode-inspection-current.json'}elseif($FeeRules){'fee-bytecode-inspection-current.json'}else{'jar-inspection-current.json'};[IO.File]::WriteAllText((Join-Path $work $report),$json)}
    }catch{$failures.Add($_.Exception)}finally{$lease.Dispose()}
}
if($failures.Count-eq 1){[Runtime.ExceptionServices.ExceptionDispatchInfo]::Capture($failures[0]).Throw()}
if($failures.Count-gt 1){throw [AggregateException]::new('Offline inspection/cleanup failed; original error first.',$failures)}
Write-Output $json
