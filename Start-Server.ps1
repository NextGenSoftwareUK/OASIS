# PowerShell script to start the STAR Web UI backend server
Write-Host "Starting STAR Web UI Backend Server..." -ForegroundColor Green

# Navigate to the WebUI directory
Set-Location "STAR ODK\NextGenSoftware.OASIS.STAR.WebUI"

# Start the server
Write-Host "Starting server on http://localhost:50563..." -ForegroundColor Yellow
dotnet run --urls "http://localhost:50563"
