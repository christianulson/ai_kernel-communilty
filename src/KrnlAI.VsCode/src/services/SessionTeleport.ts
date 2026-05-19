import * as vscode from 'vscode';

export interface TeleportSession {
    sessionId: string;
    history: ChatMessage[];
    workingMemory: Record<string, unknown>;
    activeGoalIds: string[];
    createdAt: string;
    lastActiveAt: string;
}

export interface ChatMessage {
    role: 'user' | 'assistant' | 'system';
    content: string;
    timestamp: string;
}

export type TeleportStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export class SessionTeleport implements vscode.Disposable {
    private _connection: unknown = null;
    private _status: TeleportStatus = 'disconnected';
    private _relayUrl = '';
    private _sessionHandler: ((session: TeleportSession) => void) | null = null;
    private _disposed = false;
    private _reconnectTimer: ReturnType<typeof setTimeout> | null = null;
    private _abortController: AbortController | null = null;
    private _eventSource: EventSource | null = null;
    private readonly _reconnectDelayMs = 5000;

    public get status(): TeleportStatus {
        return this._status;
    }

    public async connect(relayUrl: string): Promise<void> {
        if (this._disposed || this._status === 'connecting') return;

        this._relayUrl = relayUrl.replace(/\/+$/, '');
        this._status = 'connecting';

        try {
            const signalR = await this.tryLoadSignalR();
            if (signalR && !this._disposed) {
                await this.connectSignalR(signalR);
                return;
            }
        } catch {
            /* fall through to SSE */
        }

        if (!this._disposed) {
            this.connectSSE();
        }
    }

    public async teleportIn(sessionId: string): Promise<TeleportSession> {
        const url = `${this._relayUrl}/api/v1/session-teleport/${encodeURIComponent(sessionId)}/claim`;
        const response = await fetch(url, { method: 'POST' });
        if (!response.ok) {
            throw new Error(`SessionTeleport: claim failed for ${sessionId} — ${response.status} ${response.statusText}`);
        }
        return (await response.json()) as TeleportSession;
    }

    public async teleportOut(session: TeleportSession): Promise<void> {
        const url = `${this._relayUrl}/api/v1/session-teleport/${encodeURIComponent(session.sessionId)}`;
        const response = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(session),
        });
        if (!response.ok) {
            throw new Error(`SessionTeleport: push failed for ${session.sessionId} — ${response.status} ${response.statusText}`);
        }
    }

    public onSessionReceived(callback: (session: TeleportSession) => void): void {
        this._sessionHandler = callback;
    }

    public async disconnect(): Promise<void> {
        if (this._reconnectTimer) {
            clearTimeout(this._reconnectTimer);
            this._reconnectTimer = null;
        }

        this._abortController?.abort();
        this._abortController = null;

        this._eventSource?.close();
        this._eventSource = null;

        if (this._connection) {
            try {
                await (this._connection as { stop: () => Promise<void> }).stop();
            } catch {
                /* ignore */
            }
            this._connection = null;
        }

        this._status = 'disconnected';
    }

    public dispose(): void {
        this._disposed = true;
        this.disconnect();
    }

    private async connectSignalR(signalR: { HubConnectionBuilder: new () => { withUrl: (url: string) => { withAutomaticReconnect: (delays: number[]) => { build: () => unknown } } } }): Promise<void> {
        try {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl(`${this._relayUrl}/hubs/session-relay`)
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();

            connection.on('SessionTeleported', (sessionJson: string) => {
                try {
                    const session: TeleportSession = JSON.parse(sessionJson);
                    this._sessionHandler?.(session);
                } catch {
                    /* malformed payload */
                }
            });

            connection.onreconnecting(() => {
                this._status = 'connecting';
            });

            connection.onreconnected(() => {
                this._status = 'connected';
            });

            connection.onclose(() => {
                this._status = 'disconnected';
                this.scheduleReconnect();
            });

            await connection.start();
            this._connection = connection;
            this._status = 'connected';
        } catch {
            this._status = 'error';
            this.scheduleReconnect();
        }
    }

    private connectSSE(): void {
        try {
            this._abortController = new AbortController();
            const url = `${this._relayUrl}/api/v1/session-teleport/stream`;

            if (typeof EventSource !== 'undefined') {
                this._eventSource = new EventSource(url);
                this._eventSource.onopen = () => {
                    this._status = 'connected';
                };
                this._eventSource.onmessage = (event: MessageEvent) => {
                    try {
                        const session: TeleportSession = JSON.parse(event.data);
                        this._sessionHandler?.(session);
                    } catch {
                        /* malformed payload */
                    }
                };
                this._eventSource.onerror = () => {
                    this._eventSource?.close();
                    this._status = 'error';
                    this.scheduleReconnect();
                };
            } else {
                this.startPolling(url);
            }
        } catch {
            this._status = 'error';
            this.scheduleReconnect();
        }
    }

    private async startPolling(url: string): Promise<void> {
        this._status = 'connected';

        const poll = async () => {
            if (this._disposed) return;

            try {
                const response = await fetch(url, {
                    signal: this._abortController?.signal,
                });

                if (!response.ok) {
                    this._status = 'error';
                    this.scheduleReconnect();
                    return;
                }

                const body = await response.json();
                if (body && Array.isArray(body.sessions)) {
                    for (const session of body.sessions as TeleportSession[]) {
                        this._sessionHandler?.(session);
                    }
                }
            } catch (err: unknown) {
                if (err instanceof DOMException && err.name === 'AbortError') return;
                this._status = 'error';
                this.scheduleReconnect();
                return;
            }

            if (!this._disposed) {
                setTimeout(poll, 5000);
            }
        };

        poll();
    }

    private scheduleReconnect(): void {
        if (this._disposed) return;
        this._reconnectTimer = setTimeout(() => {
            this._reconnectTimer = null;
            this.connect(this._relayUrl);
        }, this._reconnectDelayMs);
    }

    private async tryLoadSignalR(): Promise<{ HubConnectionBuilder: unknown } | null> {
        try {
            return await import('@microsoft/signalr');
        } catch {
            return null;
        }
    }
}
