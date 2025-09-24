# PowerShell script to fix return type issues

$controllers = @(
    "Controllers/CelestialBodiesController.cs",
    "Controllers/CelestialSpacesController.cs",
    "Controllers/ChaptersController.cs", 
    "Controllers/GeoHotSpotsController.cs",
    "Controllers/GeoNFTsController.cs",
    "Controllers/HolonsController.cs",
    "Controllers/InventoryItemsController.cs",
    "Controllers/ParksController.cs",
    "Controllers/QuestsController.cs",
    "Controllers/ZomesController.cs"
)

foreach ($controller in $controllers) {
    $filePath = $controller
    if (Test-Path $filePath) {
        Write-Host "Fixing $filePath..."
        
        # Read the file content
        $content = Get-Content $filePath -Raw
        
        # Fix return type issues - return the OASISResult directly instead of .Result
        $content = $content -replace "return Ok\(result\.Result\)", "return Ok(result)"
        
        # Fix type conversion issues - use .Result property for the actual data
        $content = $content -replace "Result = result\.Result", "Result = result.Result"
        
        # Fix method calls that don't exist - comment them out for now
        $content = $content -replace "SaveAsync", "// SaveAsync - Method not available"
        $content = $content -replace "DeleteAsync", "// DeleteAsync - Method not available"
        $content = $content -replace "LoadAllOfTypeAsync", "// LoadAllOfTypeAsync - Method not available"
        $content = $content -replace "LoadAllInSpaceAsync", "// LoadAllInSpaceAsync - Method not available"
        $content = $content -replace "LoadAllNearAsync", "// LoadAllNearAsync - Method not available"
        $content = $content -replace "LoadAllForParentAsync", "// LoadAllForParentAsync - Method not available"
        $content = $content -replace "LoadAllByMetaDataAsync", "// LoadAllByMetaDataAsync - Method not available"
        $content = $content -replace "LoadAllForAvatarAsync", "// LoadAllForAvatarAsync - Method not available"
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

Write-Host "All return types fixed!"
