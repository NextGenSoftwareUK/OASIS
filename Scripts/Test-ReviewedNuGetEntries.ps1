# In-memory archive regression checks; creates no build/download/test-output files.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Publish-OASISReviewedNuGet.ps1'), [ref]$null, [ref]$errors)
if ($errors) { throw $errors }
$function = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-PackageEntries'
}, $true)
Invoke-Expression $function.Extent.Text
function New-TestArchive([string]$Payload, [string]$Signature) {
    $memory = [IO.MemoryStream]::new()
    $zip = [IO.Compression.ZipArchive]::new($memory, [IO.Compression.ZipArchiveMode]::Create, $true)
    foreach ($item in @(@('lib/example.dll', $Payload), @('.signature.p7s', $Signature))) {
        $entry = $zip.CreateEntry($item[0])
        $writer = [IO.StreamWriter]::new($entry.Open())
        $writer.Write($item[1]); $writer.Dispose()
    }
    $zip.Dispose()
    $bytes = $memory.ToArray(); $memory.Dispose()
    return ,$bytes
}
$original = Get-PackageEntries (New-TestArchive 'payload' 'original-signature')
$resigned = Get-PackageEntries (New-TestArchive 'payload' 'repository-signature')
$changed = Get-PackageEntries (New-TestArchive 'changed-payload' 'repository-signature')
if ($original.Count -ne 1 -or $original.ContainsKey('.signature.p7s')) { throw 'Signing exclusion failed.' }
if ($original['lib/example.dll'] -ne $resigned['lib/example.dll']) { throw 'Re-signing should preserve content identity.' }
if ($original['lib/example.dll'] -eq $changed['lib/example.dll']) { throw 'Changed package content was not detected.' }
Write-Host 'PASS: signature-only difference accepted; payload difference detected; archive entry coverage retained.'
