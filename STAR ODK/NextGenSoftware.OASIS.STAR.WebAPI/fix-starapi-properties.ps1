# PowerShell script to fix STARAPI property references

$controllers = @(
    "Controllers/CelestialSpacesController.cs",
    "Controllers/ChaptersController.cs", 
    "Controllers/GeoHotSpotsController.cs",
    "Controllers/HolonsController.cs",
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
        
        # Fix STARAPI property references
        $content = $content -replace "_starAPI\.IChapters", "_starAPI.Chapters"
        $content = $content -replace "_starAPI\.IQuests", "_starAPI.Quests"
        $content = $content -replace "_starAPI\.IGeoHotSpots", "_starAPI.GeoHotSpots"
        $content = $content -replace "_starAPI\.IZomes", "_starAPI.Zomes"
        $content = $content -replace "_starAPI\.IHolons", "_starAPI.Holons"
        $content = $content -replace "_starAPI\.IParks", "_starAPI.Parks"
        $content = $content -replace "_starAPI\.ICelestialSpaces", "_starAPI.CelestialSpaces"
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

# Fix InventoryItemsController STARDNA issue
$inventoryController = "Controllers/InventoryItemsController.cs"
if (Test-Path $inventoryController) {
    Write-Host "Fixing $inventoryController..."
    $content = Get-Content $inventoryController -Raw
    
    # Add STARDNA using statement
    if ($content -notmatch "using NextGenSoftware\.OASIS\.STAR\.DNA;") {
        $content = $content -replace "(using NextGenSoftware\.OASIS\.API\.Core\.Exceptions;)", "`$1`nusing NextGenSoftware.OASIS.STAR.DNA;"
    }
    
    Set-Content $inventoryController $content -NoNewline
    Write-Host "Fixed $inventoryController"
}

Write-Host "All STARAPI property references fixed!"
