import { test, expect, BrowserContext } from '@playwright/test';
import {
  createExtensionContext,
  getExtensionId,
  getPopupPage,
  getSidebarPage,
  clickSettings,
  typeInChat,
} from './helpers';

let context: BrowserContext;
let extensionId: string;

test.beforeAll(async () => {
  context = await createExtensionContext();
  extensionId = await getExtensionId(context);
});

test.afterAll(async () => {
  await context.close();
});

test('Extension loads without errors', async () => {
  const popup = await getPopupPage(context, extensionId);
  await expect(popup.locator('#root')).toBeAttached();
  await expect(popup.locator('#root')).not.toBeEmpty();
  await popup.close();
});

test('Popup shows Krnl-AI header', async () => {
  const popup = await getPopupPage(context, extensionId);
  await expect(popup.getByText('Krnl-AI')).toBeVisible();
  await popup.close();
});

test('Settings panel opens and closes', async () => {
  const popup = await getPopupPage(context, extensionId);

  const endpointInput = popup.locator('input[placeholder="API endpoint"]');
  await expect(endpointInput).toBeHidden();

  await clickSettings(popup);
  await expect(endpointInput).toBeVisible();
  await expect(endpointInput).toHaveValue(/http:\/\/localhost:\d+/);

  await clickSettings(popup);
  await expect(endpointInput).toBeHidden();

  await popup.close();
});

test('Chat input is present', async () => {
  const popup = await getPopupPage(context, extensionId);
  const chatInput = popup.locator('input[placeholder="Type a message..."]');
  await expect(chatInput).toBeVisible();
  await expect(chatInput).toBeEnabled();

  const sendButton = popup.getByRole('button', { name: 'Send' });
  await expect(sendButton).toBeVisible();
  await expect(sendButton).toBeDisabled();

  await typeInChat(popup, 'hello');
  await expect(sendButton).toBeEnabled();

  await popup.close();
});

test('Connection status indicator', async () => {
  const popup = await getPopupPage(context, extensionId);
  const statusDot = popup.locator('div[class*="rounded-full"]').first();
  await expect(statusDot).toBeVisible();

  const validClasses = ['bg-yellow-500', 'bg-green-500', 'bg-red-500'];
  const classAttr = await statusDot.getAttribute('class');
  const hasStatusClass = validClasses.some((c) => classAttr?.includes(c));
  expect(hasStatusClass).toBe(true);

  await popup.close();
});

test('Sidebar has Chat and Memory tabs', async () => {
  const sidebar = await getSidebarPage(context, extensionId);

  const chatTab = sidebar.getByRole('button', { name: 'Chat' });
  const memoryTab = sidebar.getByRole('button', { name: 'Memory' });

  await expect(chatTab).toBeVisible();
  await expect(memoryTab).toBeVisible();

  await expect(chatTab).toHaveClass(/bg-krnl-800/);
  await expect(memoryTab).not.toHaveClass(/bg-krnl-800/);

  await memoryTab.click();
  await expect(memoryTab).toHaveClass(/bg-krnl-800/);
  await expect(chatTab).not.toHaveClass(/bg-krnl-800/);

  const memorySearch = sidebar.locator(
    'input[placeholder="Search memory..."]',
  );
  await expect(memorySearch).toBeVisible();

  await sidebar.close();
});

test('Service worker registers', async () => {
  const workers = context.serviceWorkers();
  expect(workers.length).toBeGreaterThanOrEqual(1);

  const swUrl = workers[0].url();
  expect(swUrl).toContain('chrome-extension://');
  expect(swUrl).toMatch(/(service-worker-loader|serviceWorker)/);
});

test('Context menus registered', async () => {
  const workers = context.serviceWorkers();
  expect(workers.length).toBeGreaterThanOrEqual(1);

  const sw = workers[0];
  const contextMenus = await sw.evaluate(() => {
    return new Promise<{ id: string; title: string }[]>((resolve) => {
      chrome.contextMenus?.getAll((menus) => {
        resolve(
          menus.map((m) => ({ id: m.id, title: m.title ?? m.id })),
        );
      });
    });
  });

  const menuIds = contextMenus.map((m) => m.id);
  expect(menuIds).toContain('explain-page');
  expect(menuIds).toContain('extract-memory');

  const explainItem = contextMenus.find(
    (m) => m.id === 'explain-page',
  );
  expect(explainItem?.title).toContain('Explain');

  await sw.evaluate(() => {
    return new Promise<void>((resolve) => {
      chrome.contextMenus?.removeAll(() => resolve());
    });
  });
});
