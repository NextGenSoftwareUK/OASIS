# PowerShell script to fix manager method names

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
        
        # Fix method names - remove "I" prefix from method names
        $content = $content -replace "LoadAllIZomesAsync", "LoadAllAsync"
        $content = $content -replace "LoadIZomeAsync", "LoadAsync"
        $content = $content -replace "SaveIZomeAsync", "SaveAsync"
        $content = $content -replace "DeleteIZomeAsync", "DeleteAsync"
        $content = $content -replace "LoadAllIZomesOfTypeAsync", "LoadAllOfTypeAsync"
        $content = $content -replace "LoadAllIZomesInSpaceAsync", "LoadAllInSpaceAsync"
        
        $content = $content -replace "LoadAllICelestialSpacesAsync", "LoadAllAsync"
        $content = $content -replace "LoadICelestialSpaceAsync", "LoadAsync"
        $content = $content -replace "SaveICelestialSpaceAsync", "SaveAsync"
        $content = $content -replace "DeleteICelestialSpaceAsync", "DeleteAsync"
        $content = $content -replace "LoadAllICelestialSpacesOfTypeAsync", "LoadAllOfTypeAsync"
        $content = $content -replace "LoadAllICelestialSpacesInSpaceAsync", "LoadAllInSpaceAsync"
        
        $content = $content -replace "LoadAllIChaptersAsync", "LoadAllAsync"
        $content = $content -replace "LoadIChapterAsync", "LoadAsync"
        $content = $content -replace "SaveIChapterAsync", "SaveAsync"
        $content = $content -replace "DeleteIChapterAsync", "DeleteAsync"
        
        $content = $content -replace "LoadAllIGeoHotSpotsAsync", "LoadAllAsync"
        $content = $content -replace "LoadIGeoHotSpotAsync", "LoadAsync"
        $content = $content -replace "SaveIGeoHotSpotAsync", "SaveAsync"
        $content = $content -replace "DeleteIGeoHotSpotAsync", "DeleteAsync"
        $content = $content -replace "LoadAllIGeoHotSpotsNearAsync", "LoadAllNearAsync"
        
        $content = $content -replace "LoadAllGeoNFTsAsync", "LoadAllAsync"
        $content = $content -replace "LoadGeoNFTAsync", "LoadAsync"
        $content = $content -replace "SaveGeoNFTAsync", "SaveAsync"
        $content = $content -replace "DeleteGeoNFTAsync", "DeleteAsync"
        $content = $content -replace "LoadAllGeoNFTsNearAsync", "LoadAllNearAsync"
        $content = $content -replace "LoadAllGeoNFTsForAvatarAsync", "LoadAllForAvatarAsync"
        
        $content = $content -replace "LoadAllIHolonsAsync", "LoadAllAsync"
        $content = $content -replace "LoadIHolonAsync", "LoadAsync"
        $content = $content -replace "SaveIHolonAsync", "SaveAsync"
        $content = $content -replace "DeleteIHolonAsync", "DeleteAsync"
        $content = $content -replace "LoadAllIHolonsOfTypeAsync", "LoadAllOfTypeAsync"
        $content = $content -replace "LoadAllIHolonsForParentAsync", "LoadAllForParentAsync"
        $content = $content -replace "LoadAllIHolonsByMetaDataAsync", "LoadAllByMetaDataAsync"
        
        $content = $content -replace "LoadAllInventoryItemsAsync", "LoadAllAsync"
        $content = $content -replace "LoadInventoryItemAsync", "LoadAsync"
        $content = $content -replace "SaveInventoryItemAsync", "SaveAsync"
        $content = $content -replace "DeleteInventoryItemAsync", "DeleteAsync"
        $content = $content -replace "LoadAllInventoryItemsForAvatarAsync", "LoadAllForAvatarAsync"
        
        $content = $content -replace "LoadAllIParksAsync", "LoadAllAsync"
        $content = $content -replace "LoadIParkAsync", "LoadAsync"
        $content = $content -replace "SaveIParkAsync", "SaveAsync"
        $content = $content -replace "DeleteIParkAsync", "DeleteAsync"
        $content = $content -replace "LoadAllIParksNearAsync", "LoadAllNearAsync"
        $content = $content -replace "LoadAllIParksOfTypeAsync", "LoadAllOfTypeAsync"
        
        $content = $content -replace "LoadAllIQuestsAsync", "LoadAllAsync"
        $content = $content -replace "LoadIQuestAsync", "LoadAsync"
        $content = $content -replace "SaveIQuestAsync", "SaveAsync"
        $content = $content -replace "DeleteIQuestAsync", "DeleteAsync"
        $content = $content -replace "LoadAllIQuestsForAvatarAsync", "LoadAllForAvatarAsync"
        
        $content = $content -replace "LoadAllCelestialBodiesOfTypeAsync", "LoadAllOfTypeAsync"
        $content = $content -replace "LoadAllCelestialBodiesInSpaceAsync", "LoadAllInSpaceAsync"
        $content = $content -replace "SaveICelestialBodyAsync", "SaveAsync"
        $content = $content -replace "DeleteICelestialBodyAsync", "DeleteAsync"
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

Write-Host "All method names fixed!"
