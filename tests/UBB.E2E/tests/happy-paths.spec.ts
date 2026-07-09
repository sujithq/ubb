import { test, expect } from '@playwright/test';

/**
 * Happy-path E2E tests (TD-18)
 *
 * One run-and-assert test per simulation mode, plus a blocked-outcome preset.
 * Complements url-sharing.spec.ts — these verify the simulation itself
 * produces visible results in the browser.
 */

test.describe('Simulation happy paths', () => {
  test('Single mode: Run produces a pass result and log entries', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Default state: request fits in ULB and pool → free pool usage
    await page.getByRole('button', { name: 'Run', exact: true }).click();
    await page.waitForTimeout(500);

    // Result node shows the free-pool outcome with the pass icon
    const resultNode = page.locator('.ubb-node', { hasText: 'Result' });
    await expect(resultNode).toContainText('Free pool usage');
    await expect(resultNode).toHaveClass(/is-pass/);

    // Execution log received PASS entries
    const logLines = page.locator('.ubb-log-line');
    await expect(logLines.first()).toBeVisible();
    await expect(page.locator('.ubb-log-pass').first()).toBeVisible();
  });

  test('Single mode: "Cost centre block" preset produces a blocked result', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.locator('button.ubb-preset-btn', { hasText: 'Cost centre block' }).click();
    await page.waitForTimeout(300);
    await page.getByRole('button', { name: 'Run', exact: true }).click();
    await page.waitForTimeout(500);

    const resultNode = page.locator('.ubb-node', { hasText: 'Result' });
    await expect(resultNode).toContainText('Request blocked');
    await expect(resultNode).toHaveClass(/is-block/);
  });

  test('Agentic mode: Run executes the workflow and logs each step', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.locator('button.ubb-mode-tab', { hasText: 'Agentic workflow' }).click();
    await page.waitForTimeout(500);

    await page.getByRole('button', { name: 'Run', exact: true }).click();
    await page.waitForTimeout(500);

    // The result node must leave the idle state ("Run a scenario.")
    const resultNode = page.locator('.ubb-node', { hasText: 'Result' });
    await expect(resultNode).not.toContainText('Run a scenario');

    // The log received at least one entry per evaluated step
    const logLines = page.locator('.ubb-log-line');
    expect(await logLines.count()).toBeGreaterThan(1);
  });

  test('Multi-CC mode: preset + run produces per-CC diagrams and a summary', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.locator('button.ubb-mode-tab', { hasText: 'Multi-cost-center' }).click();
    await page.waitForTimeout(500);

    // Select the normal preset, then run
    await page.locator('button', { hasText: 'Multi-CC Normal' }).first().click();
    await page.waitForTimeout(300);
    await page.locator('button', { hasText: 'Run Multi-CC Simulation' }).click();
    await page.waitForTimeout(800);

    // Per-CC flow diagrams appear
    await expect(page.locator('text=Cost Center Flow Diagrams')).toBeVisible();

    // Simulation log contains the summary section with all three cost centers
    await expect(page.locator('text=== SUMMARY ===')).toBeVisible();
    await expect(page.locator('text=Engineering:').first()).toBeVisible();
  });
});
