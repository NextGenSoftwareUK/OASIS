param([Parameter(Mandatory)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ArchivePath).Path)
try {
    foreach ($required in @('NextGenSoftware.OASIS.API.Core.dll', 'NextGenSoftware.OASIS.OASISBootLoader.dll', 'OASIS_DNA.json')) {
        if (@($zip.Entries | Where-Object FullName -eq $required).Count -ne 1) { throw "Missing or duplicated runtime entry: $required" }
    }
    foreach ($entry in $zip.Entries) {
        if ($entry.Name -like 'NextGenSoftware.OASIS.STAR.*' -or $entry.Name -like 'STAR.*' -or
            $entry.Name -like 'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint*') { throw "Non-runtime binary leaked: $($entry.FullName)" }
        if ($entry.Name -match '(?i)OASIS[_-]?DNA.*\.json$' -and $entry.FullName -ne 'OASIS_DNA.json') { throw 'Configured DNA leaked into archive.' }
    }
    $templatePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json'
    $bytes = [IO.MemoryStream]::new()
    $entryStream = $zip.GetEntry('OASIS_DNA.json').Open()
    try {
        $entryStream.CopyTo($bytes)
        if ([Convert]::ToBase64String($bytes.ToArray()) -ne [Convert]::ToBase64String([IO.File]::ReadAllBytes($templatePath))) {
            throw 'Runtime DNA differs from the checked-in Default template.'
        }
    } finally { $entryStream.Dispose(); $bytes.Dispose() }
} finally { $zip.Dispose() }
Write-Output 'Runtime archive contract passed: dependencies present, STAR excluded, exact Default DNA template.'
