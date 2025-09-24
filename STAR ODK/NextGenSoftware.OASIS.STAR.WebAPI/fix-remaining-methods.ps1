# Fix remaining broken method calls
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Fix remaining broken method calls
    $content = $content -replace "// LoadAllNearAsync - Method not available", "LoadAllNearAsync"
    $content = $content -replace "// LoadAllInRadiusAsync - Method not available", "LoadAllInRadiusAsync"
    $content = $content -replace "// LoadAllByTypeAsync - Method not available", "LoadAllByTypeAsync"
    $content = $content -replace "// LoadAllByStatusAsync - Method not available", "LoadAllByStatusAsync"
    $content = $content -replace "// LoadAllByOwnerAsync - Method not available", "LoadAllByOwnerAsync"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Fixed remaining methods in: $($file.Name)"
}
