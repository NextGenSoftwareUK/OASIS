# Fix method calls with correct signatures
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Fix LoadAllAsync calls with correct parameters
    $content = $content -replace "LoadAllAsync\(Guid\.Empty, null\)", "LoadAllAsync(Guid.Empty, null, true, false, 0)"
    
    # Fix LoadAsync calls with correct parameters  
    $content = $content -replace "LoadAsync\(Guid\.Empty, ([^)]+)\)", "LoadAsync(Guid.Empty, `$1, 0)"
    
    # Replace SaveAsync calls with NotImplementedException for now
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.SaveAsync\([^)]*\);", "throw new NotImplementedException(\"SaveAsync method not yet implemented - use SaveHolonAsync instead\");"
    
    # Replace DeleteAsync calls with correct signature
    $content = $content -replace "DeleteAsync\(([^)]+)\)", "DeleteAsync(Guid.Empty, `$1, 0)"
    
    # Replace other missing methods with NotImplementedException
    $content = $content -replace "LoadAllOfTypeAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllOfTypeAsync method not yet implemented\");"
    $content = $content -replace "LoadAllInSpaceAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllInSpaceAsync method not yet implemented\");"
    $content = $content -replace "LoadAllNearAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllNearAsync method not yet implemented\");"
    $content = $content -replace "LoadAllForAvatarAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllForAvatarAsync method not yet implemented\");"
    $content = $content -replace "LoadAllForParentAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllForParentAsync method not yet implemented\");"
    $content = $content -replace "LoadAllByMetaDataAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllByMetaDataAsync method not yet implemented\");"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Fixed method signatures in: $($file.Name)"
}
