#!/bin/bash

echo "Testing Milestone 1: Device Catalog Handling"

# Start API in background
echo "Starting API..."
"/mnt/c/Program Files/dotnet/dotnet.exe" run --project dingoConfig.api --no-build &
API_PID=$!
sleep 5

echo "Testing catalog endpoints..."

# Test status endpoint
echo "GET /api/catalogs/status"
curl -s http://localhost:5089/api/catalogs/status | jq .

# Test catalogs list
echo -e "\nGET /api/catalogs"
curl -s http://localhost:5089/api/catalogs | jq .

# Test device types
echo -e "\nGET /api/catalogs/types" 
curl -s http://localhost:5089/api/catalogs/types | jq .

# Test specific catalog
echo -e "\nGET /api/catalogs/ExampleDevice"
curl -s http://localhost:5089/api/catalogs/ExampleDevice | jq .

# Test validation
echo -e "\nPOST /api/catalogs/validate"
curl -s -X POST http://localhost:5089/api/catalogs/validate \
  -H "Content-Type: application/json" \
  -d '{"filePath": "../catalogs/ExampleDevice.json"}' | jq .

# Stop API
echo -e "\nStopping API..."
kill $API_PID 2>/dev/null

echo "Milestone 1 testing completed!"