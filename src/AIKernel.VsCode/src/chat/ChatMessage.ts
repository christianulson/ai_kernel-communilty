export interface ChatMessage {
    id: string;
    role: 'user' | 'assistant' | 'system';
    content: string;
    timestamp: number;
    isStreaming?: boolean;
    isError?: boolean;
    metadata?: {
        command?: string;
        contextFile?: string;
    };
}

export function createMessage(
    role: ChatMessage['role'],
    content: string,
    metadata?: ChatMessage['metadata']
): ChatMessage {
    return {
        id: `msg_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`,
        role,
        content,
        timestamp: Date.now(),
        metadata
    };
}
