# Fix type conversion errors by returning OASISResult instead of result.Result
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Fix type conversion errors by returning the full OASISResult
    $content = $content -replace "return Ok\(result\.Result\);", "return Ok(result);"
    $content = $content -replace "return Ok\(new OASISResult<[^>]+>\s*\{[^}]+\}\);", "return Ok(result);"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Fixed type conversions in: $($file.Name)"
}
