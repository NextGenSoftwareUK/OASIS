Param(
    [string]$InputDir = ".",
    [string]$OutDir = ".",
    [switch]$Install
)

if ($Install) {
  npm i -D @mermaid-js/mermaid-cli
}

$files = Get-ChildItem -Path $InputDir -Filter *.mmd -Recurse
foreach ($f in $files) {
  $out = Join-Path $OutDir ($f.BaseName + ".svg")
  Write-Host "Rendering $($f.FullName) -> $out"
  $config = Join-Path $PSScriptRoot "puppeteer-config.json"
  npx --yes @mermaid-js/mermaid-cli -i $f.FullName -o $out --backgroundColor white --scale 1 --puppeteerConfigFile $config
}
