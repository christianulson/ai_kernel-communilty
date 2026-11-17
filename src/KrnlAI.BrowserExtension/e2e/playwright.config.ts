import { defineConfig, chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');

// Use the Playwright-managed Chromium (Chrome for Testing). Branded Chrome no
// longer honors --load-extension on recent versions.
const CHROME_PATH = process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || chromium.executablePath();

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
    headless: process.env.PLAYWRIGHT_HEADLESS === '1',
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
          executablePath: CHROME_PATH,
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
