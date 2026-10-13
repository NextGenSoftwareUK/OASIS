param([switch]$Remove)
$ErrorActionPreference='Stop'
$root='C:\Source\OASIS'
if((Resolve-Path -LiteralPath $root).Path -ne $root){throw 'Unexpected primary checkout path'}
$busy=Get-CimInstance Win32_Process | Where-Object {
    $_.ProcessId -ne $PID -and $_.Name -match '^(dotnet|MSBuild|testhost|devenv|Unity)\.exe$' -and
    $_.CommandLine -and $_.CommandLine.Contains($root,[StringComparison]::OrdinalIgnoreCase)
}
if($busy){throw 'Primary checkout has active build/editor/test processes; preserving output'}
$projects=Get-ChildItem -LiteralPath $root -Filter '*.csproj' -File -Recurse | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\'
}
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$removed=0L
foreach($project in $projects){
    foreach($name in @('bin','obj')){
        $target=Join-Path $project.DirectoryName $name
        if(!(Test-Path -LiteralPath $target) -or !$seen.Add($target)){continue}
        $resolved=(Resolve-Path -LiteralPath $target).Path
        if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase) -or
            $resolved -ne $target -or (Get-Item -LiteralPath $target).Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)){
            throw "Unexpected or linked generated directory: $target"
        }
        $tracked=& git -C $project.DirectoryName ls-files -- $name
        if($LASTEXITCODE -ne 0){throw 'Tracking audit failed'}
        if($tracked){Write-Output "Preserving tracked output: $target";continue}
        $files=@(Get-ChildItem -LiteralPath $target -Recurse -File)
        if($files | Where-Object {$_.Name -match '^\.env($|\.)|credential|private.key|identity.dpapi' -or $_.Extension -in @('.pem','.pfx','.key','.dpapi')}){
            Write-Output "Preserving potential credentials: $target";continue
        }
        # Preserve nested dependency junctions and non-build source trees.
        if(Get-ChildItem -LiteralPath $target -Recurse -Force | Where-Object {$_.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)}){
            Write-Output "Preserving linked output: $target";continue
        }
        $bytes=($files | Measure-Object Length -Sum).Sum
        if($Remove){Remove-Item -LiteralPath $target -Recurse -Force; $removed+=$bytes}
        [pscustomobject]@{Path=$target;MiB=[math]::Round($bytes/1MB,2);Removed=[bool]$Remove}|ConvertTo-Json -Compress
    }
}
[pscustomobject]@{RemovedGiB=[math]::Round($removed/1GB,2);FreeGiB=[math]::Round((Get-PSDrive C).Free/1GB,2)}|ConvertTo-Json -Compress
