# Test STAR Web UI API
Write-Host "Testing STAR Web UI API..." -ForegroundColor Green

# Test status endpoint
Write-Host "`n1. Testing status endpoint..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-WebRequest -Uri "http://localhost:50563/api/star/status" -UseBasicParsing
    Write-Host "Status: $($statusResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Content: $($statusResponse.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test ignite endpoint
Write-Host "`n2. Testing ignite endpoint..." -ForegroundColor Yellow
try {
    $igniteBody = @{
        userName = "admin"
        password = "admin"
    } | ConvertTo-Json
    
    $igniteResponse = Invoke-WebRequest -Uri "http://localhost:50563/api/star/ignite" -Method POST -ContentType "application/json" -Body $igniteBody -UseBasicParsing
    Write-Host "Status: $($igniteResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Content: $($igniteResponse.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Yellow
    }
}

Write-Host "`nAPI Test Complete!" -ForegroundColor Green




