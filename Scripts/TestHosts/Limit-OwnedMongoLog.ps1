function Limit-OwnedMongoLog {
    param([Parameter(Mandatory)][string]$WorkDirectory)
    $directory=[IO.Path]::GetFullPath($WorkDirectory)
    $log=Join-Path $directory 'mongo-current.log'
    $tail=Join-Path $directory 'mongo-current-tail.log'
    if(!(Test-Path -LiteralPath $log -PathType Leaf)){return}
    foreach($path in @($directory,$log,$tail)){
        if((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)){
            throw 'Refusing log retention through a reparse point.'
        }
    }
    # The runner owns its environment lease and has no live test Mongo at this boundary.
    # Exclusive access additionally refuses an in-use file. Preserve a bounded tail
    # before truncation; any read/write failure leaves the original log untouched.
    $stream=[IO.File]::Open($log,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try {
        if($stream.Length -le 8MB){return}
        $before=$stream.Length
        $count=[int][Math]::Min(64KB,$before)
        $buffer=[byte[]]::new($count)
        [void]$stream.Seek(-$count,[IO.SeekOrigin]::End)
        $stream.ReadExactly($buffer,0,$count)
        # Start at the next full JSON log line, avoiding partial UTF-8/JSON records.
        $newline=[Array]::IndexOf($buffer,[byte]10)
        $start=if($newline -ge 0){$newline+1}else{$count}
        $output=[IO.File]::Open($tail,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try {$output.Write($buffer,$start,$count-$start);$output.Flush($true)} finally {$output.Dispose()}
        $stream.SetLength(0)
        $stream.Flush($true)
        Write-Output "Owned Mongo log bounded: raw bytes=$before, retained tail bytes=$($count-$start). Database/runtime untouched."
    } finally {$stream.Dispose()}
}
