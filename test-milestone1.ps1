# Test script for Milestone 1: Device Catalog Handling

Write-Host "Testing Milestone 1: Device Catalog Handling" -ForegroundColor Green

# Test catalog loading
Write-Host "`nTesting catalog loading..." -ForegroundColor Yellow
$catalogService = New-Object dingoConfig.Persistence.Services.DeviceCatalogService
try {
    dotnet run --project dingoConfig.api --no-build &
    Start-Sleep 5
    
    # Test API endpoints
    Write-Host "Testing GET /api/catalogs/status" -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "http://localhost:5089/api/catalogs/status" -Method GET
    Write-Host "Status: $($response | ConvertTo-Json)" -ForegroundColor White
    
    Write-Host "Testing GET /api/catalogs" -ForegroundColor Yellow  
    $catalogs = Invoke-RestMethod -Uri "http://localhost:5089/api/catalogs" -Method GET
    Write-Host "Catalogs: $($catalogs | ConvertTo-Json)" -ForegroundColor White
    
    Write-Host "Testing GET /api/catalogs/types" -ForegroundColor Yellow
    $types = Invoke-RestMethod -Uri "http://localhost:5089/api/catalogs/types" -Method GET  
    Write-Host "Device Types: $($types | ConvertTo-Json)" -ForegroundColor White
    
    if ($types.Count -gt 0) {
        $firstType = $types[0]
        Write-Host "Testing GET /api/catalogs/$firstType" -ForegroundColor Yellow
        $catalog = Invoke-RestMethod -Uri "http://localhost:5089/api/catalogs/$firstType" -Method GET
        Write-Host "Catalog: $($catalog.deviceType) v$($catalog.version)" -ForegroundColor White
    }
    
    Write-Host "`nMilestone 1 tests completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    # Stop the API
    Get-Process | Where-Object {$_.ProcessName -eq "dotnet" -and $_.CommandLine -like "*dingoConfig.api*"} | Stop-Process -Force
}