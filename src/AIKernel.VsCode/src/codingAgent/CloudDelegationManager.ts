import * as vscode from 'vscode';
import { EditorContext } from './EditorContextProvider';
import { withProgress } from '../utils/ProgressHelper';

export enum DelegationStatus {
    Pending = 'pending',
    Running = 'running',
    Completed = 'completed',
    Failed = 'failed',
}

export interface DelegationResult {
    id: string;
    status: DelegationStatus;
    result?: string;
    error?: string;
    createdAt: number;
    completedAt?: number;
}

export class CloudDelegationManager {
    private _pending = new Map<string, DelegationResult>();

    get config() {
        return vscode.workspace.getConfiguration('aikernel.cloudDelegation');
    }

    get enabled(): boolean {
        return this.config.get<boolean>('enabled', false);
    }

    get endpoint(): string {
        return this.config.get<string>('endpoint', 'https://cloud.aikernel.dev');
    }

    get maxExecutionTime(): number {
        return this.config.get<number>('maxExecutionTime', 300);
    }

    async delegate(task: string, context: EditorContext): Promise<DelegationResult> {
        if (!this.enabled) {
            return {
                id: '',
                status: DelegationStatus.Failed,
                error: 'Cloud delegation not enabled. Set aikernel.cloudDelegation.enabled = true.',
                createdAt: Date.now(),
            };
        }

        return withProgress(
            'Delegating to AI Kernel Cloud...',
            async (progress, token) => {
                const id = `delegate_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;

                const result: DelegationResult = {
                    id,
                    status: DelegationStatus.Pending,
                    createdAt: Date.now(),
                };
                this._pending.set(id, result);

                progress.report({ message: 'Sending task to cloud...', increment: 10 });

                const apiKey = this.config.get<string>('apiKey', '');
                const body = {
                    task,
                    context: {
                        activeFile: context.activeFile,
                        language: context.language,
                        content: context.content?.substring(0, 50000),
                        diagnostics: context.diagnostics.slice(0, 20),
                    },
                    maxExecutionTime: this.maxExecutionTime,
                };

                try {
                    const response = await fetch(`${this.endpoint}/api/delegate`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': apiKey ? `Bearer ${apiKey}` : '',
                        },
                        body: JSON.stringify(body),
                        signal: AbortSignal.timeout(10_000),
                    });

                    if (!response.ok) {
                        const errText = await response.text().catch(() => 'Unknown error');
                        result.status = DelegationStatus.Failed;
                        result.error = `Cloud returned ${response.status}: ${errText}`;
                        this._pending.set(id, result);
                        return result;
                    }

                    const data = await response.json();
                    const delegateId = data.id || id;

                    progress.report({ message: 'Task running in cloud...', increment: 30 });

                    result.status = DelegationStatus.Running;
                    result.id = delegateId;
                    this._pending.set(id, result);

                    const pollIntervalMs = 2000;
                    const maxAttempts = (this.maxExecutionTime * 1000) / pollIntervalMs;
                    let attempts = 0;

                    while (attempts < maxAttempts) {
                        if (token.isCancellationRequested) {
                            result.status = DelegationStatus.Failed;
                            result.error = 'Cancelled by user';
                            this._pending.set(id, result);
                            return result;
                        }

                        await new Promise(r => setTimeout(r, pollIntervalMs));
                        attempts++;

                        progress.report({
                            message: `Waiting for cloud... (${attempts * 2}s)`,
                            increment: Math.min(5, 60 / maxAttempts),
                        });

                        const statusResponse = await fetch(
                            `${this.endpoint}/api/delegate/${delegateId}/status`,
                            {
                                headers: { 'Authorization': apiKey ? `Bearer ${apiKey}` : '' },
                                signal: AbortSignal.timeout(5000),
                            }
                        );

                        if (!statusResponse.ok) continue;

                        const statusData = await statusResponse.json();

                        if (statusData.status === 'completed') {
                            result.status = DelegationStatus.Completed;
                            result.result = statusData.result || statusData.narration;
                            result.completedAt = Date.now();
                            this._pending.set(id, result);
                            return result;
                        }

                        if (statusData.status === 'failed') {
                            result.status = DelegationStatus.Failed;
                            result.error = statusData.error || 'Cloud execution failed';
                            result.completedAt = Date.now();
                            this._pending.set(id, result);
                            return result;
                        }
                    }

                    result.status = DelegationStatus.Failed;
                    result.error = `Timeout after ${this.maxExecutionTime}s`;
                    result.completedAt = Date.now();
                    this._pending.set(id, result);
                    return result;
                } catch (err: any) {
                    result.status = DelegationStatus.Failed;
                    result.error = `Connection error: ${err.message}`;
                    this._pending.set(id, result);
                    return result;
                }
            }
        );
    }

    getDelegation(id: string): DelegationResult | undefined {
        return this._pending.get(id);
    }

    listDelegations(): DelegationResult[] {
        return Array.from(this._pending.values())
            .sort((a, b) => b.createdAt - a.createdAt);
    }

    clearCompleted(): void {
        for (const [id, result] of this._pending) {
            if (result.status === DelegationStatus.Completed || result.status === DelegationStatus.Failed) {
                this._pending.delete(id);
            }
        }
    }
}
