# Test Server Startup
Write-Host "Testing server startup..." -ForegroundColor Green

# Kill any existing dotnet processes
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Start the server in foreground to see any errors
Write-Host "Starting server..." -ForegroundColor Yellow
dotnet run --urls "http://localhost:5000"




