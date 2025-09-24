# Fix broken method calls that were replaced with comments
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Fix broken method calls that were replaced with comments
    $content = $content -replace "// SaveAsync - Method not available", "SaveAsync"
    $content = $content -replace "// DeleteAsync - Method not available", "DeleteAsync"
    $content = $content -replace "// LoadAsync - Method not available", "LoadAsync"
    $content = $content -replace "// LoadAllAsync - Method not available", "LoadAllAsync"
    $content = $content -replace "// LoadAllOfTypeAsync - Method not available", "LoadAllOfTypeAsync"
    $content = $content -replace "// LoadAllInSpaceAsync - Method not available", "LoadAllInSpaceAsync"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Fixed broken methods in: $($file.Name)"
}
