import { defineConfig } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import os from 'os';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');

const pwUserDir =
  process.env.PLAYWRIGHT_BROWSERS_PATH ||
  path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright');

function findChromiumExecutable(): string | undefined {
  const candidates = [
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    path.join(os.homedir(), 'AppData', 'Local', 'Google', 'Chrome', 'Application', 'chrome.exe'),
  ];

  const pwCandidates = [
    path.join(pwUserDir, 'chromium-1223', 'chrome-win64', 'chrome.exe'),
    path.join(pwUserDir, 'chromium-1181', 'chrome-win64', 'chrome.exe'),
    path.join(pwUserDir, 'chromium-1179', 'chrome-win64', 'chrome.exe'),
  ];

  for (const c of [...candidates, ...pwCandidates]) {
    try {
      if (require('fs').existsSync(c)) return c;
    } catch {
      // continue
    }
  }
  return undefined;
}

const chromePath = findChromiumExecutable();

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.spec.ts',
  timeout: 30000,
  retries: 0,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report' }],
  ],
  use: {
    headless: false,
    viewport: { width: 1280, height: 720 },
    actionTimeout: 10000,
    screenshot: process.env.CI ? 'only-on-failure' : 'off',
    video: process.env.CI ? 'retain-on-failure' : 'off',
    trace: process.env.CI ? 'retain-on-failure' : 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        browserName: 'chromium',
        launchOptions: {
          executablePath: chromePath,
          args: [
            `--disable-extensions-except=${EXTENSION_PATH}`,
            `--load-extension=${EXTENSION_PATH}`,
            '--no-sandbox',
            '--disable-gpu',
          ],
        },
      },
    },
  ],
});
