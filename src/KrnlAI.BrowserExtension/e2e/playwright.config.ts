import { defineConfig } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EXTENSION_PATH = path.resolve(__dirname, '..', 'dist');

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
    headless: !process.env.CI ? false : true,
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
