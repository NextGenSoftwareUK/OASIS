# PowerShell script to fix common controller issues

Write-Host "Fixing controller issues..."

# Function to fix a controller file
function Fix-Controller {
    param($FilePath)
    
    Write-Host "Fixing $FilePath"
    
    # Read the file content
    $content = Get-Content $FilePath -Raw
    
    # Fix 1: Change ControllerBase to STARControllerBase
    $content = $content -replace 'public class (\w+)Controller : ControllerBase', 'public class $1Controller : STARControllerBase'
    
    # Fix 2: Replace hardcoded Guid.Empty with AvatarId
    $content = $content -replace 'Guid\.Parse\("00000000-0000-0000-0000-000000000000"\)', 'AvatarId'
    
    # Fix 3: Fix SaveAsync to UpdateAsync with proper parameters
    $content = $content -replace 'SaveAsync\(([^)]+)\)', 'UpdateAsync(AvatarId, $1)'
    
    # Fix 4: Fix DeleteAsync to include proper parameters
    $content = $content -replace 'DeleteAsync\(([^,)]+)\)', 'DeleteAsync(AvatarId, $1, 0)'
    
    # Fix 5: Add missing using statement for concrete types
    if ($content -notmatch 'using NextGenSoftware\.OASIS\.API\.ONODE\.Core\.Holons;') {
        $content = $content -replace '(using NextGenSoftware\.OASIS\.API\.ONODE\.Core\.Interfaces\.Holons;)', '$1' + "`nusing NextGenSoftware.OASIS.API.ONODE.Core.Holons;"
    }
    
    # Write the fixed content back
    Set-Content $FilePath -Value $content -NoNewline
}

# Fix all controller files
$controllers = @(
    "Controllers/ChaptersController.cs",
    "Controllers/GeoHotSpotsController.cs", 
    "Controllers/GeoNFTsController.cs",
    "Controllers/HolonsController.cs",
    "Controllers/InventoryItemsController.cs",
    "Controllers/MissionsController.cs",
    "Controllers/ParksController.cs"
)

foreach ($controller in $controllers) {
    if (Test-Path $controller) {
        Fix-Controller $controller
    }
}

Write-Host "Controller fixes completed!"


