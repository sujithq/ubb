# Local E2E test runner for UBB Simulator URL Sharing
# This script sets up and runs Playwright tests locally

$ErrorActionPreference = "Stop"

Write-Host "🧪 UBB Simulator E2E Test Suite" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if npm is installed
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "❌ npm not found. Please install Node.js first." -ForegroundColor Red
    exit 1
}

# Navigate to E2E test directory
$e2eDir = Join-Path $PSScriptRoot "tests\UBB.E2E"
if (-not (Test-Path $e2eDir)) {
    Write-Host "❌ E2E test directory not found at $e2eDir" -ForegroundColor Red
    exit 1
}

Push-Location $e2eDir

try {
    Write-Host ""
    Write-Host "📦 Installing Playwright dependencies..." -ForegroundColor Yellow
    npm install --no-progress

    Write-Host ""
    Write-Host "🎭 Downloading Playwright browsers..." -ForegroundColor Yellow
    npx playwright install --with-deps chromium

    Write-Host ""
    Write-Host "🚀 Running E2E tests..." -ForegroundColor Yellow
    Write-Host "   (The .NET app will be started automatically)" -ForegroundColor Gray

    npm test -- --reporter=list

    Write-Host ""
    Write-Host "✅ E2E tests completed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 View detailed report:" -ForegroundColor Cyan
    Write-Host "   npm run report" -ForegroundColor Gray
}
finally {
    Pop-Location
}
