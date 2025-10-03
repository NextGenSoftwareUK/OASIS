# Start STAR Web UI Server
Write-Host "Starting STAR Web UI Server..." -ForegroundColor Green
Write-Host "Server will be available at: http://localhost:5000" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Cyan
Write-Host ""

# Start the server
dotnet run --urls "http://localhost:5000"

