# UBB Simulator E2E Tests

End-to-end tests for URL sharing functionality across all simulation modes.

## Setup

```bash
cd tests/UBB.E2E
npm install
```

## Run Tests

```bash
# Run all tests
npm test

# Run in UI mode (interactive)
npm run test:ui

# Run with headed browser (see what's happening)
npm run test:headed

# Debug a specific test
npm run test:debug

# View HTML report
npm run report
```

## What Gets Tested

### TD-02: Single & Agentic Mode URL Sharing
- Serialize state to URL-safe format
- Share button copies URL to clipboard
- Opening shared URL restores state
- Preset preservation in URL

### TD-03: Multi-Cost-Center URL Sharing
- Serialize multi-CC state including all cost centers
- Restore mode (Single, Agentic, Multi-CC)
- Restore multi-CC budget values
- Restore cost center configurations

### URL Format & Compatibility
- Base64URL encoding (URL-safe characters only)
- No `+`, `/`, `=` characters in hash
- Backward compatibility with TD-02 URLs
- Long URL handling for multi-CC scenarios

### Integration Tests
- Mode switches preserve state
- Toast notifications on share
- State consistency across operations

## Coverage

- ✅ Single-request mode sharing
- ✅ Agentic-flow mode sharing  
- ✅ Multi-cost-center mode sharing
- ✅ Preset preservation
- ✅ Cost center budget restoration
- ✅ URL format validation
- ✅ Toast notifications

## Continuous Integration

These tests run automatically:
- On every pull request to `main`
- Before merging (enforced by branch protection rules)
- Locally before committing (recommended)

## Troubleshooting

**Tests timeout waiting for app:**
- Ensure the .NET app is running or configured to start via `webServer` in `playwright.config.ts`
- Check that port 5000 is available

**Selector not found:**
- The app layout may have changed; update selectors in the test file
- Use `npx playwright codegen` to generate selectors from live app

**Clipboard access fails:**
- Some test runners don't allow clipboard access; tests gracefully skip this assertion

## Adding New Tests

1. Create a new `test.describe()` block
2. Use `test('should...')` for each test case
3. Interact with the page using `page.locator()`, `page.goto()`, etc.
4. Assert expected behavior with `expect()`
5. Run `npm test` to verify
