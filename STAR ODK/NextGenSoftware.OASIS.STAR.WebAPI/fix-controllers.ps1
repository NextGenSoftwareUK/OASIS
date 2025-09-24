# PowerShell script to fix all controller using statements

$controllers = @(
    "Controllers/CelestialBodiesController.cs",
    "Controllers/CelestialSpacesController.cs", 
    "Controllers/ChaptersController.cs",
    "Controllers/GeoHotSpotsController.cs",
    "Controllers/GeoNFTsController.cs",
    "Controllers/HolonsController.cs",
    "Controllers/InventoryItemsController.cs",
    "Controllers/MissionsController.cs",
    "Controllers/NFTsController.cs",
    "Controllers/OAPPsController.cs",
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
        
        # Add missing using statements if they don't exist
        if ($content -notmatch "using NextGenSoftware\.OASIS\.Common;") {
            $content = $content -replace "(using Microsoft\.AspNetCore\.Mvc;)", "`$1`nusing NextGenSoftware.OASIS.Common;"
        }
        
        if ($content -notmatch "using NextGenSoftware\.OASIS\.API\.Core\.Exceptions;") {
            $content = $content -replace "(using NextGenSoftware\.OASIS\.Common;)", "`$1`nusing NextGenSoftware.OASIS.API.Core.Exceptions;"
        }
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

Write-Host "All controllers fixed!"
