import { ApprovalManager, ApprovalMode, PendingApproval } from '../codingAgent/ApprovalManager';
import { formatApprovalMessage, shouldRequestApproval } from '../chat/AutoReviewUI';

describe('AutoReviewUI', () => {
    describe('formatApprovalMessage', () => {
        it('ShouldFormatApprovalForWebview', () => {
            const approval: PendingApproval = {
                id: 'test-1',
                action: 'edit_file test.ts',
                details: ['Editing line 42'],
                timestamp: Date.now(),
                deadline: Date.now() + 30000,
                status: 'pending'
            };
            const msg = formatApprovalMessage(approval);
            expect(msg.type).toBe('approval');
            expect(msg.id).toBe('test-1');
            expect(msg.action).toBe('edit_file test.ts');
            expect(msg.details).toEqual(['Editing line 42']);
            expect(msg.remaining).toBeGreaterThan(0);
        });
    });

    describe('shouldRequestApproval', () => {
        it('ChatMode_ShouldReturnFalse', () => {
            expect(shouldRequestApproval(ApprovalMode.Chat, 'anything')).toBe(false);
        });

        it('SafeMode_ShouldReturnTrue', () => {
            expect(shouldRequestApproval(ApprovalMode.SafeAgent, 'edit_file test.ts')).toBe(true);
        });

        it('FullMode_WithAllowedTool_ShouldReturnFalse', () => {
            expect(shouldRequestApproval(ApprovalMode.FullAgent, 'read_file test.ts')).toBe(false);
        });

        it('FullMode_WithDisallowedTool_ShouldReturnTrue', () => {
            expect(shouldRequestApproval(ApprovalMode.FullAgent, 'delete_file test.ts')).toBe(true);
        });
    });
});
