import { BrowserContext, chromium, Page } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import os from 'os';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');

function findChromiumExecutable(): string | undefined {
  const candidates = [
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    path.join(os.homedir(), 'AppData', 'Local', 'Google', 'Chrome', 'Application', 'chrome.exe'),
  ];
  const pwUserDir =
    process.env.PLAYWRIGHT_BROWSERS_PATH ||
    path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright');
  for (const ver of ['chromium-1223', 'chromium-1181', 'chromium-1179']) {
    candidates.push(path.join(pwUserDir, ver, 'chrome-win64', 'chrome.exe'));
  }
  for (const c of candidates) {
    try { if (fs.existsSync(c)) return c; } catch { /* ignore */ }
  }
  return undefined;
}

const chromeExecutable = findChromiumExecutable();

export async function createExtensionContext(
  headless = false,
): Promise<BrowserContext> {
  const userDataDir = path.resolve(__dirname, `.user-data-${Date.now()}`);
  const context = await chromium.launchPersistentContext(userDataDir, {
    executablePath: chromeExecutable,
    headless,
    args: [
      `--disable-extensions-except=${EXTENSION_PATH}`,
      `--load-extension=${EXTENSION_PATH}`,
      '--no-sandbox',
      '--disable-gpu',
    ],
  });
  return context;
}

export async function getExtensionId(
  context: BrowserContext,
): Promise<string> {
  // Navigate to about:blank to trigger service worker registration
  const page = context.pages()[0] || (await context.newPage());
  await page.goto('about:blank');

  // Poll for the extension service worker
  for (let attempt = 0; attempt < 20; attempt++) {
    const workers = context.serviceWorkers();
    for (const sw of workers) {
      const match = sw.url().match(/chrome-extension:\/\/([a-z]{32})/i);
      if (match) return match[1];
    }
    // Navigate again to trigger the SW
    await page.goto('about:blank').catch(() => {});
    await new Promise((r) => setTimeout(r, 1500));
  }

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
