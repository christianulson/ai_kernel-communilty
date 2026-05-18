import * as vscode from 'vscode';

export interface CognitiveEvent {
    type: string;
    data: unknown;
    timestamp: string;
}

export type SignalRStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export class SignalRClient implements vscode.Disposable {
    private _connection: unknown = null;
    private _status: SignalRStatus = 'disconnected';
    private _statusBarItem: vscode.StatusBarItem;
    private _reconnectTimer: ReturnType<typeof setTimeout> | null = null;
    private _eventHandlers: Map<string, Array<(event: CognitiveEvent) => void>> = new Map();
    private _disposed = false;

    public get status(): SignalRStatus { return this._status; }
    private readonly _serverUrl: string;
    private readonly _reconnectDelayMs = 5000;

    constructor() {
        const config = vscode.workspace.getConfiguration('krnlAI');
        this._serverUrl = config.get<string>('signalrUrl', 'http://localhost:5000');
        this._statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 99);
        this._statusBarItem.tooltip = 'Krnl-AI SignalR Status';
        this.updateStatusBar();
        this._statusBarItem.show();
    }

    public async connect(): Promise<void> {
        if (this._disposed || this._status === 'connecting') return;
        this._status = 'connecting';
        this.updateStatusBar();

        try {
            // Dynamic import to avoid requiring @microsoft/signalr at compile time
            const signalR = await this.loadSignalRLibrary();
            if (!signalR || this._disposed) {
                this.fallbackToPolling();
                return;
            }

            const connection = new signalR.HubConnectionBuilder()
                .withUrl(`${this._serverUrl}/hub/cognitive-stream`)
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();

            connection.on('CognitiveEvent', (event: CognitiveEvent) => {
                this.dispatchEvent(event);
            });

            connection.onreconnecting(() => {
                this._status = 'connecting';
                this.updateStatusBar();
            });

            connection.onreconnected(() => {
                this._status = 'connected';
                this.updateStatusBar();
            });

            connection.onclose(() => {
                this._status = 'disconnected';
                this.updateStatusBar();
                this.scheduleReconnect();
            });

            await connection.start();
            this._connection = connection;
            this._status = 'connected';
            this.updateStatusBar();
        } catch {
            this._status = 'error';
            this.updateStatusBar();
            this.fallbackToPolling();
        }
    }

    public on(eventType: string, handler: (event: CognitiveEvent) => void): vscode.Disposable {
        if (!this._eventHandlers.has(eventType)) {
            this._eventHandlers.set(eventType, []);
        }
        this._eventHandlers.get(eventType)!.push(handler);
        return { dispose: () => this.off(eventType, handler) };
    }

    public off(eventType: string, handler: (event: CognitiveEvent) => void): void {
        const handlers = this._eventHandlers.get(eventType);
        if (handlers) {
            const idx = handlers.indexOf(handler);
            if (idx >= 0) handlers.splice(idx, 1);
        }
    }

    public async disconnect(): Promise<void> {
        if (this._reconnectTimer) {
            clearTimeout(this._reconnectTimer);
            this._reconnectTimer = null;
        }
        if (this._connection) {
            try {
                await (this._connection as { stop: () => Promise<void> }).stop();
            } catch { /* ignore */ }
            this._connection = null;
        }
        this._status = 'disconnected';
        this.updateStatusBar();
    }

    public dispose(): void {
        this._disposed = true;
        this.disconnect();
        this._statusBarItem.dispose();
        this._eventHandlers.clear();
    }

    private dispatchEvent(event: CognitiveEvent): void {
        const handlers = this._eventHandlers.get(event.type) ?? this._eventHandlers.get('*');
        if (handlers) {
            for (const handler of handlers) {
                try { handler(event); } catch { /* handler error */ }
            }
        }
    }

    private updateStatusBar(): void {
        const icons: Record<SignalRStatus, string> = {
            'connected': '$(plug)',
            'connecting': '$(sync~spin)',
            'disconnected': '$(plug)',
            'error': '$(warning)'
        };
        const colors: Record<SignalRStatus, string> = {
            'connected': '#4ecdc4',
            'connecting': '#ffe66d',
            'disconnected': '#888',
            'error': '#ff6b6b'
        };
        this._statusBarItem.text = `${icons[this._status]} SignalR`;
        this._statusBarItem.color = colors[this._status];
    }

    private scheduleReconnect(): void {
        if (this._disposed) return;
        this._reconnectTimer = setTimeout(() => {
            this._reconnectTimer = null;
            this.connect();
        }, this._reconnectDelayMs);
    }

    private fallbackToPolling(): void {
        this._statusBarItem.text = '$(circuit-board) Polling';
        this._statusBarItem.color = '#888';
        this._statusBarItem.tooltip = 'SignalR unavailable — using REST polling';
    }

    private async loadSignalRLibrary(): Promise<{ HubConnectionBuilder: unknown } | null> {
        try {
            // Try dynamic import of @microsoft/signalr
            const mod = await import('@microsoft/signalr');
            return mod;
        } catch {
            return null;
        }
    }
}
