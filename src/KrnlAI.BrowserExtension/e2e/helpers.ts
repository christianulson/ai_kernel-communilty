import { BrowserContext, chromium, Page } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import os from 'os';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');

// Prefer Playwright Chromium (Chrome for Testing) over system Chrome for reliable extension testing
const CHROME_PATH: string = (() => {
  const pwDir =
    process.env.PLAYWRIGHT_BROWSERS_PATH ||
    path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright');

  const candidates = [
    // Playwright Chromium first (Chrome for Testing — best for extensions + service workers)
    path.join(pwDir, 'chromium-1223', 'chrome-win64', 'chrome.exe'),
    path.join(pwDir, 'chromium-1228', 'chrome-win64', 'chrome.exe'),
    path.join(pwDir, 'chromium-1229', 'chrome-win64', 'chrome.exe'),
    // Fallback to system Chrome
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    path.join(os.homedir(), 'AppData', 'Local', 'Google', 'Chrome', 'Application', 'chrome.exe'),
  ];

  for (const p of candidates) {
    try { if (fs.existsSync(p)) return p; } catch { /* ignore */ }
  }
  return '';
})();

if (!CHROME_PATH) {
  throw new Error(
    'No Chromium/Chrome executable found. Install Playwright Chromium or set CHROME_TEST_PATH.',
  );
}

export async function createExtensionContext(
  headless = false,
): Promise<{ context: BrowserContext; extensionId: string }> {
  const userDataDir = path.resolve(__dirname, `.user-data-${Date.now()}`);
  const context = await chromium.launchPersistentContext(userDataDir, {
    executablePath: CHROME_PATH,
    headless,
    args: [
      `--disable-extensions-except=${EXTENSION_PATH}`,
      `--load-extension=${EXTENSION_PATH}`,
      '--no-sandbox',
      '--disable-gpu',
    ],
  });

  const extensionId = await resolveExtensionId(context);
  return { context, extensionId };
}

async function resolveExtensionId(
  context: BrowserContext,
): Promise<string> {
  const page = context.pages()[0] || (await context.newPage());
  await page.goto('about:blank');

  for (let attempt = 0; attempt < 20; attempt++) {
    const workers = context.serviceWorkers();
    for (const sw of workers) {
      const match = sw.url().match(/chrome-extension:\/\/([a-z]{32})/i);
      if (match) return match[1];
    }
    await page.goto('about:blank').catch(() => {});
    await new Promise((r) => setTimeout(r, 1500));
  }

  await context.close();
  throw new Error(
    'Could not determine extension ID. Ensure the extension builds without errors at dist/',
  );
}

export async function getPopupPage(
  context: BrowserContext,
  extensionId: string,
): Promise<Page> {
  const page = await context.newPage();
  await page.goto(
    `chrome-extension://${extensionId}/src/popup/index.html`,
    { waitUntil: 'domcontentloaded' },
  );
  await page.waitForSelector('#root', { state: 'attached', timeout: 10000 });
  return page;
}

export async function getSidebarPage(
  context: BrowserContext,
  extensionId: string,
): Promise<Page> {
  const page = await context.newPage();
  await page.goto(
    `chrome-extension://${extensionId}/src/sidebar/index.html`,
    { waitUntil: 'domcontentloaded' },
  );
  await page.waitForSelector('#root', { state: 'attached', timeout: 10000 });
  return page;
}

export async function waitForConnectionStatus(page: Page): Promise<void> {
  await page.waitForSelector('[class*="rounded-full"]', { timeout: 10000 });
}

export async function clickSettings(page: Page): Promise<void> {
  await page.getByRole('button', { name: '⚙' }).click();
}

export async function typeInChat(page: Page, text: string): Promise<void> {
  const input = page.locator('input[placeholder="Type a message..."]');
  await input.fill(text);
}
