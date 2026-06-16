import React, { useEffect, useRef, useState } from 'react';
import { KrnlAiClient } from '../lib/krnlaiClient';
import { getChatHistory, saveChatHistory, getEndpoint, setEndpoint } from '../lib/storage';
import type { ChatMessage, PageContext } from '../lib/types';

const client = new KrnlAiClient();

function generateId(): string {
  return `msg_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

export default function App() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [endpoint, setEndpointState] = useState('http://localhost:5100');
  const [showSettings, setShowSettings] = useState(false);
  const [connectionOk, setConnectionOk] = useState<boolean | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    (async () => {
      const [hist, ep] = await Promise.all([getChatHistory(), getEndpoint()]);
      setMessages(hist);
      setEndpointState(ep);
      client.setBaseUrl(ep);
      const health = await client.health();
      setConnectionOk(health?.ok ?? false);
    })();
  }, []);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const addMessage = (role: ChatMessage['role'], content: string) => {
    const msg: ChatMessage = { id: generateId(), role, content, timestamp: Date.now() };
    setMessages((prev) => {
      const next = [...prev, msg];
      saveChatHistory(next);
      return next;
    });
  };

  const handleSend = async () => {
    const text = input.trim();
    if (!text || loading) return;
    setInput('');
    addMessage('user', text);

    setLoading(true);
    addMessage('assistant', '...');

    try {
      let pageCtx: PageContext | undefined;
      try {
        const ctx = await chrome.runtime.sendMessage({ type: 'GET_PAGE_CONTEXT' });
        if (ctx) pageCtx = ctx;
      } catch {
        // no active tab
      }

      const response = await client.sendMessage(text, pageCtx);
      const content = response.narration || response.error || 'No response';

      setMessages((prev) => {
        const next = prev.map((m) =>
          m.content === '...' && m.role === 'assistant'
            ? { ...m, content }
            : m,
        );
        saveChatHistory(next);
        return next;
      });
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : 'Error';
      setMessages((prev) => {
        const next = prev.map((m) =>
          m.content === '...' && m.role === 'assistant'
            ? { ...m, content: `Error: ${errMsg}` }
            : m,
        );
        saveChatHistory(next);
        return next;
      });
    } finally {
      setLoading(false);
    }
  };

  const handleSendPageContext = async () => {
    setInput((prev) => prev + '\n[Page context will be attached automatically]');
  };

  const handleSaveEndpoint = async () => {
    await setEndpoint(endpoint);
    client.setBaseUrl(endpoint);
    setShowSettings(false);
    const health = await client.health();
    setConnectionOk(health?.ok ?? false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-800 bg-gray-900">
        <div className="flex items-center gap-2">
          <div className={`w-2 h-2 rounded-full ${connectionOk === null ? 'bg-yellow-500' : connectionOk ? 'bg-green-500' : 'bg-red-500'}`} />
          <span className="font-semibold text-sm">Krnl-AI</span>
        </div>
        <button
          onClick={() => setShowSettings(!showSettings)}
          className="text-gray-400 hover:text-gray-200 text-xs px-2 py-1 rounded hover:bg-gray-800"
        >
          ⚙
        </button>
      </div>

      {/* Settings */}
      {showSettings && (
        <div className="px-3 py-2 border-b border-gray-800 bg-gray-900">
          <div className="flex gap-2">
            <input
              type="text"
              value={endpoint}
              onChange={(e) => setEndpointState(e.target.value)}
              className="flex-1 bg-gray-800 text-gray-100 text-xs px-2 py-1 rounded border border-gray-700 focus:outline-none focus:border-krnl-500"
              placeholder="API endpoint"
            />
            <button
              onClick={handleSaveEndpoint}
              className="bg-krnl-700 hover:bg-krnl-600 text-white text-xs px-3 py-1 rounded"
            >
              Save
            </button>
          </div>
        </div>
      )}

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-3 py-2 space-y-2">
        {messages.length === 0 && (
          <div className="text-gray-500 text-xs text-center mt-8">
            Send a message to start chatting with Krnl-AI
          </div>
        )}
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`p-2 rounded-lg text-sm max-w-[85%] ${
              msg.role === 'user'
                ? 'bg-krnl-800 ml-auto'
                : msg.role === 'assistant'
                ? 'bg-gray-800'
                : 'bg-gray-900 text-center mx-auto text-xs opacity-70'
            } ${msg.content === '...' ? 'animate-pulse' : ''}`}
          >
            {msg.content === '...' ? (
              <span className="text-gray-400">Thinking...</span>
            ) : (
              <div className="whitespace-pre-wrap break-words">{msg.content}</div>
            )}
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div className="border-t border-gray-800 px-3 py-2 bg-gray-900">
        <div className="flex gap-2">
          <button
            onClick={handleSendPageContext}
            className="text-gray-400 hover:text-gray-200 text-xs px-2 py-1 rounded hover:bg-gray-800"
            title="Attach page context"
          >
            📄
          </button>
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type a message..."
            className="flex-1 bg-gray-800 text-gray-100 text-sm px-3 py-2 rounded-lg border border-gray-700 focus:outline-none focus:border-krnl-500 placeholder-gray-500"
            disabled={loading}
          />
          <button
            onClick={handleSend}
            disabled={!input.trim() || loading}
            className="bg-krnl-700 hover:bg-krnl-600 disabled:opacity-40 disabled:cursor-not-allowed text-white px-3 py-2 rounded-lg text-sm"
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
}
