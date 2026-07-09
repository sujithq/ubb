#!/bin/bash
# Local E2E test runner for UBB Simulator URL Sharing
# This script sets up and runs Playwright tests locally

set -e

echo "🧪 UBB Simulator E2E Test Suite"
echo "================================"

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ npm not found. Please install Node.js first."
    exit 1
fi

# Navigate to E2E test directory
cd "$(dirname "$0")/tests/UBB.E2E"

echo ""
echo "📦 Installing Playwright dependencies..."
npm install
echo ""
echo "🎭 Downloading Playwright browsers..."
npx playwright install --with-deps chromium

echo ""
echo "🚀 Starting .NET app on http://localhost:5000..."
echo "   (This will be done automatically by Playwright)"

echo ""
echo "🧪 Running E2E tests..."
npm test -- --reporter=list

echo ""
echo "✅ E2E tests completed!"
echo ""
echo "📊 View detailed report:"
echo "   npm run report"
