# Replace missing method calls with NotImplementedException
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Replace missing method calls with NotImplementedException
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.SaveAsync\([^)]*\);", "throw new NotImplementedException(\"SaveAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.DeleteAsync\([^)]*\);", "throw new NotImplementedException(\"DeleteAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllOfTypeAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllOfTypeAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllInSpaceAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllInSpaceAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllNearAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllNearAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllForAvatarAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllForAvatarAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllForParentAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllForParentAsync method not yet implemented\");"
    $content = $content -replace "await _starAPI\.[A-Za-z]+\.LoadAllByMetaDataAsync\([^)]*\);", "throw new NotImplementedException(\"LoadAllByMetaDataAsync method not yet implemented\");"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Replaced missing methods in: $($file.Name)"
}
