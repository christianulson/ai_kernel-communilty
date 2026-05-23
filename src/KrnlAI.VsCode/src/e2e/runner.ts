import { runTests } from '@vscode/test-electron';
import * as path from 'path';

async function main() {
    const extensionRoot = path.resolve(__dirname, '..', '..');
    const testApp = path.resolve(extensionRoot, 'out', 'e2e');

    try {
        await runTests({
            version: 'stable',
            extensionDevelopmentPath: extensionRoot,
            extensionTestsPath: path.join(testApp, 'suites'),
            launchArgs: [
                '--disable-extensions',
                '--user-data-dir', path.join(testApp, '.vscode-test-userdata'),
                '--locale', 'en',
            ],
        });
    } catch (err) {
        console.error('E2E tests failed:', err);
        process.exit(1);
    }
}

main();
