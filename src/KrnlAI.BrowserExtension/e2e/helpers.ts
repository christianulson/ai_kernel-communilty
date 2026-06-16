import { BrowserContext, chromium, Page } from '@playwright/test';
import path from 'path';

const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');
const USER_DATA_DIR = path.resolve(__dirname, '.user-data');

export async function createExtensionContext(): Promise<BrowserContext> {
  const context = await chromium.launchPersistentContext(USER_DATA_DIR, {
    headless: false,
    args: [
      `--disable-extensions-except=${EXTENSION_PATH}`,
      `--load-extension=${EXTENSION_PATH}`,
      '--no-sandbox',
      '--disable-gpu',
    ],
  });
  return context;
}

export async function getExtensionId(context: BrowserContext): Promise<string> {
  const existing = context.serviceWorkers();
  if (existing.length > 0) {
    const id = extractIdFromUrl(existing[0].url());
    if (id) return id;
  }

  if (context.serviceWorkers().length === 0) {
    const [sw] = await Promise.all([
      context.waitForEvent('serviceworker', { timeout: 15000 }),
    ]);
    const id = extractIdFromUrl(sw.url());
    if (id) return id;
  }

  const page = await context.newPage();
  await page.goto('chrome://inspect/#service-workers');
  await page.waitForTimeout(2000);
  await page.close();

  const retry = context.serviceWorkers();
  if (retry.length > 0) {
    const id = extractIdFromUrl(retry[0].url());
    if (id) return id;
  }

  throw new Error(
    'Could not determine extension ID. Ensure the extension builds without errors at dist/',
  );
}

function extractIdFromUrl(url: string): string | null {
  const match = url.match(/chrome-extension:\/\/([a-z]{32})/);
  return match?.[1] ?? null;
}

export async function getPopupPage(
  context: BrowserContext,
  extensionId: string,
): Promise<Page> {
  const page = await context.newPage();
  await page.goto(
    `chrome-extension://${extensionId}/src/popup/index.html`,
    { waitUntil: 'networkidle' },
  );
  await page.waitForSelector('#root', { state: 'attached' });
  return page;
}

export async function getSidebarPage(
  context: BrowserContext,
  extensionId: string,
): Promise<Page> {
  const page = await context.newPage();
  await page.goto(
    `chrome-extension://${extensionId}/src/sidebar/index.html`,
    { waitUntil: 'networkidle' },
  );
  await page.waitForSelector('#root', { state: 'attached' });
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

export async function getServiceWorker(
  context: BrowserContext,
): Promise<Page | null> {
  const existing = context.serviceWorkers();
  if (existing.length > 0) {
    return existing[0] as unknown as Page;
  }
  return null;
}
