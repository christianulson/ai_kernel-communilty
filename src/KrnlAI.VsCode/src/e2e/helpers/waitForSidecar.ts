import * as http from 'http';

export async function waitForSidecar(
    port = 5001,
    timeoutMs = 15000,
): Promise<void> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        try {
            const ok = await healthCheck(port);
            if (ok) return;
        } catch {
            // not ready yet
        }
        await sleep(500);
    }
    throw new Error(`Sidecar not ready on port ${port} after ${timeoutMs}ms`);
}

function healthCheck(port: number): Promise<boolean> {
    return new Promise(resolve => {
        const req = http.get(`http://127.0.0.1:${port}/health`, res => {
            resolve(res.statusCode === 200);
        });
        req.on('error', () => resolve(false));
        req.setTimeout(2000, () => { req.destroy(); resolve(false); });
    });
}

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}
