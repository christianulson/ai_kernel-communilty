export enum OperationState {
    Running = 'Running',
    Completed = 'Completed',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
}

export interface OperationCall {
    id: string;
    name: string;
    arguments: string | null;
    state: OperationState;
    result: string | null;
    error: string | null;
    elapsedMs: number;
    startedAt: number;
    children: OperationCall[] | null;
}

type EventCallback = (op: OperationCall) => void;

export class OperationScope implements Disposable {
    private _tracker: OperationTracker;
    private _id: string;
    private _name: string;
    private _arguments: string | null;
    private _startedAt: number;
    private _elapsedMs: number = 0;
    private _disposed: boolean = false;
    private _hasError: boolean = false;
    private _result: string | null = null;
    private _error: string | null = null;
    private _state: OperationState = OperationState.Running;
    private _childScopes: OperationScope[] = [];
    private _isRoot: boolean;

    constructor(tracker: OperationTracker, id: string, name: string, args: string | null, isRoot: boolean = true) {
        this._tracker = tracker;
        this._id = id;
        this._name = name;
        this._arguments = args;
        this._startedAt = Date.now();
        this._isRoot = isRoot;
    }

    setResult(result: string): void {
        if (this._disposed) return;
        this._result = result;
    }

    setError(error: string): void {
        if (this._disposed) return;
        this._hasError = true;
        this._error = error;
    }

    startChild(name: string, args?: string): OperationScope {
        if (this._disposed) {
            throw new Error('Cannot start child on a disposed operation scope.');
        }
        const childId = `${this._id}.${this._childScopes.length + 1}`;
        const child = new OperationScope(this._tracker, childId, name, args ?? null, false);
        this._childScopes.push(child);
        return child;
    }

    private _toOperationCall(): OperationCall {
        let children: OperationCall[] | null = null;
        if (this._childScopes.length > 0) {
            children = this._childScopes.map(c => c._toOperationCall());
        }

        return {
            id: this._id,
            name: this._name,
            arguments: this._arguments,
            state: this._state,
            result: this._result,
            error: this._error,
            elapsedMs: this._elapsedMs,
            startedAt: this._startedAt,
            children,
        };
    }

    [Symbol.dispose](): void {
        if (this._disposed) return;
        this._disposed = true;
        this._elapsedMs = Date.now() - this._startedAt;

        // Auto-dispose any undiposed children
        for (const child of this._childScopes) {
            if (!child._disposed) {
                child._disposed = true;
                child._elapsedMs = Date.now() - child._startedAt;
                child._state = child._hasError ? OperationState.Failed : OperationState.Completed;
            }
        }

        if (this._hasError) {
            this._state = OperationState.Failed;
        } else {
            this._state = OperationState.Completed;
        }

        const completedOp = this._toOperationCall();
        if (this._isRoot) {
            this._tracker._replaceOperation(completedOp);
            this._tracker._notifyOperationCompleted(completedOp);
        }
    }
}

export class OperationTracker {
    private _history: OperationCall[] = [];
    private _counter: number = 0;
    private _onOperationStarted: EventCallback | null = null;
    private _onOperationCompleted: EventCallback | null = null;

    get history(): readonly OperationCall[] {
        return [...this._history];
    }

    get onOperationStarted(): EventCallback | null {
        return this._onOperationStarted;
    }

    set onOperationStarted(cb: EventCallback | null) {
        this._onOperationStarted = cb;
    }

    get onOperationCompleted(): EventCallback | null {
        return this._onOperationCompleted;
    }

    set onOperationCompleted(cb: EventCallback | null) {
        this._onOperationCompleted = cb;
    }

    start(name: string, args?: string): OperationScope {
        this._counter++;
        const id = `op-${this._counter}`;
        const scope = new OperationScope(this, id, name, args ?? null);

        const startedOp: OperationCall = {
            id,
            name,
            arguments: args ?? null,
            state: OperationState.Running,
            result: null,
            error: null,
            elapsedMs: 0,
            startedAt: Date.now(),
            children: null,
        };

        this._history.push(startedOp);
        this._onOperationStarted?.(startedOp);
        return scope;
    }

    clear(): void {
        this._history = [];
    }

    formatTrace(limit: number = 0): string {
        if (this._history.length === 0) {
            return 'No operations tracked yet. Use any extension feature to generate operations.';
        }

        let ops = limit > 0
            ? this._history.slice(-limit)
            : [...this._history];

        const lines: string[] = ['### Debug Trace\n'];
        for (const op of ops) {
            this._appendOperation(lines, op, 0);
        }
        lines.push(`\n**Total:** ${this._history.length} operation(s) | Showing: ${ops.length}`);
        return lines.join('\n');
    }

    private _appendOperation(lines: string[], op: OperationCall, indent: number): void {
        const prefix = '  '.repeat(indent);
        const icon = op.state === OperationState.Running ? '⏳'
            : op.state === OperationState.Completed ? '✅'
            : op.state === OperationState.Failed ? '❌'
            : op.state === OperationState.Cancelled ? '🚫' : '❓';

        const elapsed = op.elapsedMs >= 1000
            ? `${(op.elapsedMs / 1000).toFixed(1)}s`
            : `${op.elapsedMs}ms`;

        lines.push(`${prefix}${icon} **${op.name}** — ${op.state} (${elapsed})`);

        if (op.arguments !== null) {
            lines.push(`${prefix}  Args: \`${op.arguments}\``);
        }
        if (op.result !== null) {
            lines.push(`${prefix}  Result: \`${op.result}\``);
        }
        if (op.error !== null) {
            lines.push(`${prefix}  Error: \`${op.error}\``);
        }

        if (op.children !== null) {
            for (const child of op.children) {
                this._appendOperation(lines, child, indent + 1);
            }
        }
    }

    /** @internal */
    _replaceOperation(completedOp: OperationCall): void {
        const index = this._history.findIndex(o => o.id === completedOp.id);
        if (index >= 0) {
            this._history[index] = completedOp;
        }
    }

    /** @internal */
    _notifyOperationCompleted(completedOp: OperationCall): void {
        this._onOperationCompleted?.(completedOp);
    }
}
