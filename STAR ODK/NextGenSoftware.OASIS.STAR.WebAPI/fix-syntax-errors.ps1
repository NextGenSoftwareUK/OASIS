# PowerShell script to fix syntax errors caused by previous script

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
        
        # Fix the broken method calls by replacing them with proper error responses
        $content = $content -replace "// SaveAsync - Method not available", "throw new NotImplementedException(\"SaveAsync method not yet implemented\")"
        $content = $content -replace "// DeleteAsync - Method not available", "throw new NotImplementedException(\"DeleteAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllOfTypeAsync - Method not available", "throw new NotImplementedException(\"LoadAllOfTypeAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllInSpaceAsync - Method not available", "throw new NotImplementedException(\"LoadAllInSpaceAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllNearAsync - Method not available", "throw new NotImplementedException(\"LoadAllNearAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllForParentAsync - Method not available", "throw new NotImplementedException(\"LoadAllForParentAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllByMetaDataAsync - Method not available", "throw new NotImplementedException(\"LoadAllByMetaDataAsync method not yet implemented\")"
        $content = $content -replace "// LoadAllForAvatarAsync - Method not available", "throw new NotImplementedException(\"LoadAllForAvatarAsync method not yet implemented\")"
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

Write-Host "All syntax errors fixed!"
