param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
$output = [IO.Path]::GetFullPath($OutputPath)
if ($output.StartsWith($source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The runtime archive must be outside the publish directory.'
}
$files = @(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {
    $_.Name -notlike 'NextGenSoftware.OASIS.STAR.*' -and
    $_.Name -notlike 'STAR.*' -and
    $_.Name -notlike 'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint*' -and
    $_.Name -notlike '*OASISDNA*.json'
})
foreach ($required in @('NextGenSoftware.OASIS.API.Core.dll', 'NextGenSoftware.OASIS.OASISBootLoader.dll')) {
    if (-not @($files | Where-Object Name -eq $required).Count) { throw "Runtime dependency missing: $required" }
}
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($output)) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Open($output, [IO.FileMode]::CreateNew)
try {
    $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in $files) {
            $name = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
            $entry = $zip.CreateEntry($name)
            $destination = $entry.Open()
            $inputFile = $file.OpenRead()
            try { $inputFile.CopyTo($destination) } finally { $inputFile.Dispose(); $destination.Dispose() }
        }
        $dna = $zip.CreateEntry('OASISDNA.json')
        $writer = [IO.StreamWriter]::new($dna.Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write('{}') } finally { $writer.Dispose() }
    } finally { $zip.Dispose() }
} finally { $stream.Dispose() }
Write-Output "OASIS Runtime archive created: $output"
