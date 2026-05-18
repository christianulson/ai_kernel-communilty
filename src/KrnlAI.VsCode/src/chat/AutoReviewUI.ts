import { PendingApproval, ApprovalDecision, ApprovalManager, ApprovalMode } from '../codingAgent/ApprovalManager';

export function formatApprovalMessage(approval: PendingApproval): any {
    return {
        type: 'approval',
        id: approval.id,
        action: approval.action,
        details: approval.details,
        deadline: approval.deadline,
        remaining: Math.max(0, approval.deadline - Date.now())
    };
}

export function connectApprovalToChat(
    manager: ApprovalManager,
    postMessage: (msg: any) => void,
    onResponse: (id: string, decision: ApprovalDecision) => void
): () => void {
    const listener = (approval: PendingApproval) => {
        postMessage(formatApprovalMessage(approval));
    };

    manager.onPending(listener);

    return () => {
    };
}

export function shouldRequestApproval(mode: ApprovalMode, action: string): boolean {
    if (mode === ApprovalMode.Chat) return false;
    if (mode === ApprovalMode.FullAgent) {
        const allowed = ['read_file', 'edit_file', 'search_files',
            'list_directory', 'get_diagnostics', 'run_command'];
        return !allowed.some(t => action.startsWith(t));
    }
    return true;
}
