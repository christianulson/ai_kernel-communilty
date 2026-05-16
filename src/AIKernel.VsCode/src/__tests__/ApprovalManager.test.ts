import { ApprovalManager, ApprovalMode } from '../codingAgent/ApprovalManager';

describe('ApprovalManager', () => {
    let manager: ApprovalManager;

    beforeEach(() => {
        manager = new ApprovalManager();
    });

    describe('initial state', () => {
        it('ShouldDefaultToChatMode', () => {
            expect(manager.getMode()).toBe(ApprovalMode.Chat);
        });
    });

    describe('setMode / getMode', () => {
        it('ShouldUpdateMode_WhenSet', () => {
            manager.setMode(ApprovalMode.SafeAgent);
            expect(manager.getMode()).toBe(ApprovalMode.SafeAgent);

            manager.setMode(ApprovalMode.FullAgent);
            expect(manager.getMode()).toBe(ApprovalMode.FullAgent);

            manager.setMode(ApprovalMode.Chat);
            expect(manager.getMode()).toBe(ApprovalMode.Chat);
        });
    });

    describe('requestApproval', () => {
        it('ChatMode_ShouldAutoApprove', async () => {
            manager.setMode(ApprovalMode.Chat);
            const decision = await manager.requestApproval('read_file test.ts', ['Reading test.ts']);
            expect(decision).toBe('allowed');
        });

        it('FullAgentMode_WithAllowedTool_ShouldAutoApprove', async () => {
            manager.setMode(ApprovalMode.FullAgent);
            const decision = await manager.requestApproval('read_file test.ts', ['Reading test.ts']);
            expect(decision).toBe('allowed');
        });

        it('FullAgentMode_WithDisallowedTool_ShouldReject', async () => {
            manager.setMode(ApprovalMode.FullAgent);
            const decision = await manager.requestApproval('delete_file test.ts', ['Deleting test.ts']);
            expect(decision).toBe('rejected');
        });

        it('SafeMode_ShouldCreatePendingApproval', async () => {
            manager.setMode(ApprovalMode.SafeAgent);
            const promise = manager.requestApproval('edit_file test.ts', ['Editing line 42']);
            const pending = manager.getPendingApprovals();
            expect(pending).toHaveLength(1);
            expect(pending[0].action).toBe('edit_file test.ts');
            expect(pending[0].status).toBe('pending');
        });
    });

    describe('respond', () => {
        it('ShouldResolvePendingApproval_WhenAllowed', async () => {
            manager.setMode(ApprovalMode.SafeAgent);
            const promise = manager.requestApproval('edit_file test.ts', ['Edit']);

            const pending = manager.getPendingApprovals();
            manager.respond(pending[0].id, 'allowed');

            const result = await promise;
            expect(result).toBe('allowed');
            expect(manager.getPendingApprovals()).toHaveLength(0);
        });

        it('ShouldResolvePendingApproval_WhenRejected', async () => {
            manager.setMode(ApprovalMode.SafeAgent);
            const promise = manager.requestApproval('edit_file test.ts', ['Edit']);

            const pending = manager.getPendingApprovals();
            manager.respond(pending[0].id, 'rejected');

            const result = await promise;
            expect(result).toBe('rejected');
        });
    });

    describe('audit log', () => {
        it('ShouldLogApprovedAction', () => {
            manager.setMode(ApprovalMode.FullAgent);
            manager.requestApproval('read_file test.ts', ['Reading']);
            const log = manager.getAuditLog();
            expect(log).toHaveLength(1);
            expect(log[0].decision).toBe('auto');
            expect(log[0].action).toBe('read_file test.ts');
        });

        it('ShouldLogUserDecision', () => {
            manager.setMode(ApprovalMode.SafeAgent);
            const promise = manager.requestApproval('edit_file test.ts', ['Editing']);

            const pending = manager.getPendingApprovals();
            manager.respond(pending[0].id, 'allowed');

            const log = manager.getAuditLog();
            expect(log).toHaveLength(1);
            expect(log[0].decision).toBe('approved');
        });
    });

    describe('onPending listener', () => {
        it('ShouldNotifyListeners_WhenApprovalCreated', (done) => {
            manager.setMode(ApprovalMode.SafeAgent);
            manager.onPending((approval) => {
                expect(approval.action).toBe('edit_file test.ts');
                expect(approval.details).toEqual(['Editing line 42']);
                done();
            });
            manager.requestApproval('edit_file test.ts', ['Editing line 42']);
        });
    });
});
