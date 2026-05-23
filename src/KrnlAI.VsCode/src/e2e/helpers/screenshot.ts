import * as fs from 'fs';
import * as path from 'path';

const SCREENSHOT_DIR = path.resolve(__dirname, '..', '..', '..', 'screenshots');

export function captureScreenshot(name: string, html: string): void {
    try {
        if (!fs.existsSync(SCREENSHOT_DIR)) {
            fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
        }
        const filePath = path.join(SCREENSHOT_DIR, `${name}-${Date.now()}.html`);
        fs.writeFileSync(filePath, html, 'utf-8');
        console.log(`[screenshot] Saved: ${filePath}`);
    } catch (err) {
        console.error(`[screenshot] Failed: ${err}`);
    }
}
