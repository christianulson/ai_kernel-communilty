export enum ApprovalMode {
    Chat = 'chat',
    SafeAgent = 'safeAgent',
    FullAgent = 'fullAgent'
}

export interface PendingApproval {
    id: string;
    action: string;
    details: string[];
    timestamp: number;
    deadline: number;
    status: 'pending' | 'approved' | 'rejected';
}

export interface AuditEntry {
    action: string;
    mode: ApprovalMode;
    decision: 'auto' | 'approved' | 'rejected';
    timestamp: number;
    details: string[];
}

export type ApprovalDecision = 'allowed' | 'rejected';

type ApprovalListener = (approval: PendingApproval) => void;

const ALLOWED_TOOLS = [
    'read_file', 'edit_file', 'search_files',
    'list_directory', 'get_diagnostics', 'run_command'
];

export class ApprovalManager {
    private _mode: ApprovalMode = ApprovalMode.Chat;
    private _pending: Map<string, PendingApproval> = new Map();
    private _auditLog: AuditEntry[] = [];
    private _listeners: ApprovalListener[] = [];
    private readonly _timeoutMs = 30000;

    setMode(mode: ApprovalMode): void {
        this._mode = mode;
    }

    getMode(): ApprovalMode {
        return this._mode;
    }

    getPendingApprovals(): PendingApproval[] {
        return Array.from(this._pending.values()).filter(p => p.status === 'pending');
    }

    getAuditLog(): AuditEntry[] {
        return [...this._auditLog];
    }

    onPending(cb: ApprovalListener): void {
        this._listeners.push(cb);
    }

    async requestApproval(
        action: string,
        details: string[]
    ): Promise<ApprovalDecision> {
        if (this._mode === ApprovalMode.Chat) return 'allowed';

        if (this._mode === ApprovalMode.FullAgent) {
            const toolName = action.split(' ')[0];
            if (!ALLOWED_TOOLS.includes(toolName)) return 'rejected';
            this._auditLog.push({
                action, mode: this._mode, decision: 'auto' as const,
                timestamp: Date.now(), details
            });
            return 'allowed';
        }

        return this._promptForApproval(action, details);
    }

    respond(id: string, decision: ApprovalDecision): void {
        const pending = this._pending.get(id);
        if (!pending || pending.status !== 'pending') return;
        const mapped = decision === 'allowed' ? 'approved' as const : 'rejected' as const;
        pending.status = mapped;
        this._auditLog.push({
            action: pending.action, mode: this._mode,
            decision: mapped, timestamp: Date.now(),
            details: pending.details
        });
    }

    private _promptForApproval(
        action: string,
        details: string[]
    ): Promise<ApprovalDecision> {
        return new Promise(resolve => {
            const id = `app_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;
            const approval: PendingApproval = {
                id, action, details,
                timestamp: Date.now(),
                deadline: Date.now() + this._timeoutMs,
                status: 'pending'
            };
            this._pending.set(id, approval);

            for (const cb of this._listeners) cb(approval);

            const timeout = setTimeout(() => {
                if (approval.status === 'pending') {
                    approval.status = 'rejected';
                    this._auditLog.push({
                        action, mode: this._mode, decision: 'rejected' as const,
                        timestamp: Date.now(), details
                    });
                    this._pending.delete(id);
                    resolve('rejected');
                }
            }, this._timeoutMs);

            const checkInterval = setInterval(() => {
                if (approval.status !== 'pending') {
                    clearInterval(checkInterval);
                    clearTimeout(timeout);
                    const result: ApprovalDecision = approval.status === 'approved' ? 'allowed' : 'rejected';
                    this._pending.delete(id);
                    resolve(result);
                }
            }, 100);
        });
    }
}
