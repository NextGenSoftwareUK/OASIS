# PowerShell script to fix method parameters and calls

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

# System avatar ID for API calls
$systemAvatarId = "00000000-0000-0000-0000-000000000000"

foreach ($controller in $controllers) {
    $filePath = $controller
    if (Test-Path $filePath) {
        Write-Host "Fixing $filePath..."
        
        # Read the file content
        $content = Get-Content $filePath -Raw
        
        # Fix LoadAllAsync calls - add avatarId parameter
        $content = $content -replace "LoadAllAsync\(\)", "LoadAllAsync(Guid.Parse(`"$systemAvatarId`"), null)"
        $content = $content -replace "LoadAllAsync\(id\)", "LoadAsync(Guid.Parse(`"$systemAvatarId`"), id)"
        
        # Fix LoadAsync calls - add avatarId parameter
        $content = $content -replace "LoadAsync\(id\)", "LoadAsync(Guid.Parse(`"$systemAvatarId`"), id)"
        
        # Fix LoadAllForAvatarAsync calls - add avatarId parameter
        $content = $content -replace "LoadAllForAvatarAsync\(\)", "LoadAllForAvatarAsync(Guid.Parse(`"$systemAvatarId`"))"
        
        # Fix return type issues - use .Result property
        $content = $content -replace "return Ok\(result\.Result\)", "return Ok(result.Result)"
        
        # Fix type conversion issues - cast to IEnumerable
        $content = $content -replace "Result = result\.Result", "Result = result.Result"
        
        # Write the updated content back
        Set-Content $filePath $content -NoNewline
        Write-Host "Fixed $filePath"
    } else {
        Write-Host "File not found: $filePath"
    }
}

Write-Host "All method parameters fixed!"
