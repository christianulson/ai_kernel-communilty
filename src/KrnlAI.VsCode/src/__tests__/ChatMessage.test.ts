import { createMessage, ChatMessage } from '../chat/ChatMessage';

describe('ChatMessage', () => {
    describe('createMessage', () => {
        it('ShouldCreateUserMessage', () => {
            const msg = createMessage('user', 'Hello');
            expect(msg.role).toBe('user');
            expect(msg.content).toBe('Hello');
            expect(msg.id).toMatch(/^msg_\d+_/);
            expect(typeof msg.timestamp).toBe('number');
        });

        it('ShouldCreateAssistantMessage', () => {
            const msg = createMessage('assistant', 'Resposta');
            expect(msg.role).toBe('assistant');
            expect(msg.content).toBe('Resposta');
        });

        it('ShouldCreateSystemMessage', () => {
            const msg = createMessage('system', 'System message');
            expect(msg.role).toBe('system');
            expect(msg.content).toBe('System message');
        });

        it('ShouldIncludeMetadata_WhenProvided', () => {
            const metadata = { command: '/explain', contextFile: '/test/file.ts' };
            const msg = createMessage('user', 'test', metadata);
            expect(msg.metadata).toEqual(metadata);
        });

        it('ShouldGenerateUniqueIds', () => {
            const msg1 = createMessage('user', 'a');
            const msg2 = createMessage('user', 'b');
            expect(msg1.id).not.toBe(msg2.id);
        });
    });

    describe('ChatMessage interface', () => {
        it('ShouldSupportOptionalStreamingFlag', () => {
            const msg: ChatMessage = {
                id: '1',
                role: 'assistant',
                content: 'test',
                timestamp: 100,
                isStreaming: true
            };
            expect(msg.isStreaming).toBe(true);
        });

        it('ShouldSupportOptionalErrorFlag', () => {
            const msg: ChatMessage = {
                id: '1',
                role: 'assistant',
                content: 'error',
                timestamp: 100,
                isError: true
            };
            expect(msg.isError).toBe(true);
        });
    });
});
