param([Parameter(Mandatory)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ArchivePath).Path)
try {
    foreach ($required in @('NextGenSoftware.OASIS.API.Core.dll', 'NextGenSoftware.OASIS.OASISBootLoader.dll', 'OASISDNA.json')) {
        if (@($zip.Entries | Where-Object FullName -eq $required).Count -ne 1) { throw "Missing or duplicated runtime entry: $required" }
    }
    foreach ($entry in $zip.Entries) {
        if ($entry.Name -like 'NextGenSoftware.OASIS.STAR.*' -or $entry.Name -like 'STAR.*' -or
            $entry.Name -like 'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint*') { throw "Non-runtime binary leaked: $($entry.FullName)" }
        if ($entry.Name -like '*OASISDNA*.json' -and $entry.FullName -ne 'OASISDNA.json') { throw 'Configured DNA leaked into archive.' }
    }
    $reader = [IO.StreamReader]::new($zip.GetEntry('OASISDNA.json').Open())
    try { if ($reader.ReadToEnd() -ne '{}') { throw 'Runtime DNA must be an empty JSON object.' } } finally { $reader.Dispose() }
} finally { $zip.Dispose() }
Write-Output 'Runtime archive contract passed: dependencies present, STAR excluded, empty DNA.'
