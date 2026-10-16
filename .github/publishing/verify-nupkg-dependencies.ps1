# Fails if any package in the folder depends on a package version that is neither on nuget.org nor in the folder itself.
param([Parameter(Mandatory)][string]$Folder)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$batch = @{}
$packages = Get-ChildItem $Folder -Filter *.nupkg
foreach ($p in $packages) { if ($p.BaseName -match '^(.+?)\.(\d+\.\d+\.\d+.*)$') { $batch["$($Matches[1].ToLower())|$($Matches[2])"] = $true } }
$cache = @{}
$missing = @()
foreach ($p in $packages) {
  $zip = [System.IO.Compression.ZipFile]::OpenRead($p.FullName)
  try { $nuspec = [xml](New-Object System.IO.StreamReader(($zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1).Open())).ReadToEnd() }
  finally { $zip.Dispose() }
  foreach ($d in $nuspec.SelectNodes("//*[local-name()='dependency']")) {
    $id = $d.id.ToLower()
    $v = ($d.version -replace '[\[\]\(\)]', '').Split(',')[0].Trim()
    if ($batch.ContainsKey("$id|$v")) { continue }
    if (-not $cache.ContainsKey($id)) {
      try { $cache[$id] = (Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$id/index.json").versions } catch { $cache[$id] = @() }
    }
    if (-not ($cache[$id] | Where-Object { $_ -eq $v -or $_ -like "$v.*" -or [version]::TryParse($_, [ref]$null) -and [version]$_ -ge [version]($v -replace '-.*','') })) {
      $missing += "$($p.Name) -> $($d.id) $v"
    }
  }
}
if ($missing.Count) { $missing | ForEach-Object { Write-Host "UNRESOLVABLE: $_" }; exit 1 }
Write-Host "All dependencies of $($packages.Count) packages resolve on nuget.org or in this batch."
