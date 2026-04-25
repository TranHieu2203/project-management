import { test, expect } from '@playwright/test';

test('home shows API OK', async ({ page }) => {
  page.on('pageerror', (err) => console.log('pageerror', err));
  page.on('console', (msg) => console.log('console', msg.type(), msg.text()));
  page.on('requestfailed', (req) => console.log('requestfailed', req.url(), req.failure()?.errorText));
  page.on('request', (req) => {
    if (req.url().includes('/api/')) console.log('request', req.method(), req.url());
  });

  await page.goto('/');
  await expect(page.getByTestId('home-page')).toBeVisible();
  await expect(page.getByTestId('api-ok')).toBeVisible({ timeout: 15000 });
});

