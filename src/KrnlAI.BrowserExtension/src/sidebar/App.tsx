import React, { useCallback, useEffect, useRef, useState } from 'react';
import { KrnlAiClient } from '../lib/krnlaiClient';
import { getChatHistory, saveChatHistory, getEndpoint, setEndpoint, clearChatHistory } from '../lib/storage';
import { extractPageContent } from '../lib/contextExtractor';
import type { ChatMessage, MemoryHit, PageContext, SlashCommandDef } from '../lib/types';
import { SLASH_COMMANDS } from '../lib/types';

const client = new KrnlAiClient();

function generateId(): string {
  return `msg_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

type Tab = 'chat' | 'memory';

export default function App() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [endpoint, setEndpointState] = useState('http://localhost:5100');
  const [showSettings, setShowSettings] = useState(false);
  const [connectionOk, setConnectionOk] = useState<boolean | null>(null);
  const [activeTab, setActiveTab] = useState<Tab>('chat');
  const [memoryQuery, setMemoryQuery] = useState('');
  const [memoryResults, setMemoryResults] = useState<MemoryHit[] | null>(null);
  const [memoryLoading, setMemoryLoading] = useState(false);
  const [showCommands, setShowCommands] = useState(false);
  const [pageContext, setPageContext] = useState<PageContext | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const loadMessages = useCallback(async () => {
    const [hist, ep] = await Promise.all([getChatHistory('sidebar'), getEndpoint()]);
    setMessages(hist);
    setEndpointState(ep);
    client.setBaseUrl(ep);
  }, []);

  useEffect(() => {
    loadMessages();
    checkHealth();

    try {
      const ctx = extractPageContent();
      setPageContext(ctx);
    } catch {
      // not in a content script context
    }

    const handler = (msg: { type: string }) => {
      if (msg.type === 'SIDEBAR_RESPONSE' || msg.type === 'EXTRACT_RESULT') {
        // Handled via background relay
      }
    };
    chrome.runtime.onMessage.addListener(handler);
    return () => chrome.runtime.onMessage.removeListener(handler);
  }, [loadMessages]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const checkHealth = async () => {
    const ep = await getEndpoint();
    client.setBaseUrl(ep);
    const health = await client.health();
    setConnectionOk(health?.ok ?? false);
  };

  const addMessage = (role: ChatMessage['role'], content: string) => {
    const msg: ChatMessage = { id: generateId(), role, content, timestamp: Date.now() };
    setMessages((prev) => {
      const next = [...prev, msg];
      saveChatHistory(next, 'sidebar');
      return next;
    });
  };

  const handleSend = async () => {
    const text = input.trim();
    if (!text || loading) return;
    setInput('');
    setShowCommands(false);
    addMessage('user', text);

    setLoading(true);

    try {
      const isSlash = text.startsWith('/');
      let response;

      if (isSlash) {
        const [cmd, ...argsArr] = text.split(' ');
        const args = argsArr.join(' ');

        switch (cmd) {
          case '/resume': {
            const content = pageContext?.text || args;
            response = await client.resumePage(content);
            break;
          }
          case '/translate': {
            const targetLang = args || 'english';
            const content = pageContext?.text || '';
            response = await client.sendMessage(
              `Translate the following content to ${targetLang}, preserving formatting:\n\n${content.substring(0, 30_000)}`,
            );
            break;
          }
          case '/extract': {
            const title = pageContext?.title || 'Browser Extract';
            const text = pageContext?.text || args;
            const ok = await client.extractToMemory(text, pageContext?.url, title);
            response = { narration: ok ? `✅ Extracted to memory: "${title}"` : '❌ Failed to extract' };
            break;
          }
          case '/ask': {
            const question = args || text;
            const ctx = pageContext ? `Page: ${pageContext.title}\n${pageContext.url}\n\n${pageContext.text.substring(0, 20_000)}` : '';
            response = await client.sendMessage(`${question}\n\n${ctx ? `Context:\n${ctx}` : ''}`);
            break;
          }
          default:
            response = await client.sendMessage(text, pageContext || undefined);
        }
      } else {
        response = await client.sendMessage(text, pageContext || undefined);
      }

      const content = response.narration || response.error || 'No response';
      setMessages((prev) => {
        const next = [...prev, { id: generateId(), role: 'assistant' as const, content, timestamp: Date.now() }];
        saveChatHistory(next, 'sidebar');
        return next;
      });
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : 'Error';
      addMessage('assistant', `Error: ${errMsg}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSlashSelect = (cmd: SlashCommandDef) => {
    setInput(cmd.id + ' ');
    setShowCommands(false);
    inputRef.current?.focus();
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInput(val);
    setShowCommands(val.startsWith('/') && !val.includes(' '));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
    if (e.key === 'Escape') {
      setShowCommands(false);
    }
  };

  const handleSearchMemory = async () => {
    const q = memoryQuery.trim();
    if (!q) return;
    setMemoryLoading(true);
    try {
      const result = await client.searchMemory(q);
      setMemoryResults(result?.hits || []);
    } catch {
      setMemoryResults([]);
    } finally {
      setMemoryLoading(false);
    }
  };

  const handleSaveEndpoint = async () => {
    await setEndpoint(endpoint);
    client.setBaseUrl(endpoint);
    setShowSettings(false);
    await checkHealth();
  };

  const handleClear = () => {
    setMessages([]);
    clearChatHistory('sidebar');
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-800 bg-gray-900 shrink-0">
        <div className="flex items-center gap-2">
          <div className={`w-2 h-2 rounded-full ${connectionOk === null ? 'bg-yellow-500' : connectionOk ? 'bg-green-500' : 'bg-red-500'}`} />
          <span className="font-semibold text-sm">Krnl-AI</span>
          <span className={`text-[10px] ${connectionOk ? 'text-green-400' : 'text-red-400'}`}>
            {connectionOk ? 'Connected' : 'Disconnected'}
          </span>
        </div>
        <div className="flex gap-1">
          <button
            onClick={() => setActiveTab('chat')}
            className={`text-xs px-2 py-1 rounded ${activeTab === 'chat' ? 'bg-krnl-800 text-white' : 'text-gray-400 hover:text-gray-200'}`}
          >
            Chat
          </button>
          <button
            onClick={() => setActiveTab('memory')}
            className={`text-xs px-2 py-1 rounded ${activeTab === 'memory' ? 'bg-krnl-800 text-white' : 'text-gray-400 hover:text-gray-200'}`}
          >
            Memory
          </button>
          <button
            onClick={() => setShowSettings(!showSettings)}
            className="text-gray-400 hover:text-gray-200 text-xs px-2 py-1 rounded hover:bg-gray-800"
          >
            ⚙
          </button>
        </div>
      </div>

      {/* Settings */}
      {showSettings && (
        <div className="px-3 py-2 border-b border-gray-800 bg-gray-900 shrink-0">
          <div className="flex gap-2 mb-2">
            <input
              type="text"
              value={endpoint}
              onChange={(e) => setEndpointState(e.target.value)}
              className="flex-1 bg-gray-800 text-gray-100 text-xs px-2 py-1 rounded border border-gray-700 focus:outline-none focus:border-krnl-500"
              placeholder="API endpoint"
            />
            <button onClick={handleSaveEndpoint} className="bg-krnl-700 hover:bg-krnl-600 text-white text-xs px-3 py-1 rounded">
              Save
            </button>
          </div>
          <button onClick={handleClear} className="text-red-400 hover:text-red-300 text-xs">
            Clear chat history
          </button>
        </div>
      )}

      {/* Tab: Chat */}
      {activeTab === 'chat' && (
        <>
          <div className="flex-1 overflow-y-auto px-3 py-2 space-y-2">
            {pageContext && (
              <div className="bg-gray-900 border border-gray-800 rounded p-2 text-xs text-gray-400 mb-2">
                <span className="text-krnl-400 font-medium">Page:</span>{' '}
                {pageContext.title || 'Untitled'}
              </div>
            )}

            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`p-2 rounded-lg text-sm max-w-[90%] ${
                  msg.role === 'user'
                    ? 'bg-krnl-800 ml-auto'
                    : msg.role === 'assistant'
                    ? 'bg-gray-800'
                    : 'bg-gray-900 text-center mx-auto text-xs opacity-70'
                }`}
              >
                <div className="whitespace-pre-wrap break-words">{msg.content}</div>
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>

          {/* Slash command suggestions */}
          {showCommands && (
            <div className="border-t border-gray-800 bg-gray-900 max-h-32 overflow-y-auto shrink-0">
              {SLASH_COMMANDS.map((cmd) => (
                <button
                  key={cmd.id}
                  onClick={() => handleSlashSelect(cmd)}
                  className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-800 flex items-center gap-2"
                >
                  <span className="text-krnl-400 font-medium">{cmd.id}</span>
                  <span className="text-gray-400">{cmd.description}</span>
                </button>
              ))}
            </div>
          )}

          {/* Input */}
          <div className="border-t border-gray-800 px-3 py-2 bg-gray-900 shrink-0">
            <div className="flex gap-2">
              <input
                ref={inputRef}
                type="text"
                value={input}
                onChange={handleInputChange}
                onKeyDown={handleKeyDown}
                placeholder="Type / for commands..."
                className="flex-1 bg-gray-800 text-gray-100 text-sm px-3 py-2 rounded-lg border border-gray-700 focus:outline-none focus:border-krnl-500 placeholder-gray-500"
                disabled={loading}
              />
              <button
                onClick={handleSend}
                disabled={!input.trim() || loading}
                className="bg-krnl-700 hover:bg-krnl-600 disabled:opacity-40 disabled:cursor-not-allowed text-white px-3 py-2 rounded-lg text-sm"
              >
                {loading ? '...' : 'Send'}
              </button>
            </div>
          </div>
        </>
      )}

      {/* Tab: Memory */}
      {activeTab === 'memory' && (
        <div className="flex-1 flex flex-col p-3 gap-3 overflow-hidden">
          <div className="flex gap-2 shrink-0">
            <input
              type="text"
              value={memoryQuery}
              onChange={(e) => setMemoryQuery(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearchMemory()}
              placeholder="Search memory..."
              className="flex-1 bg-gray-800 text-gray-100 text-sm px-3 py-2 rounded-lg border border-gray-700 focus:outline-none focus:border-krnl-500 placeholder-gray-500"
            />
            <button
              onClick={handleSearchMemory}
              disabled={!memoryQuery.trim() || memoryLoading}
              className="bg-krnl-700 hover:bg-krnl-600 disabled:opacity-40 text-white px-3 py-2 rounded-lg text-sm"
            >
              {memoryLoading ? '...' : 'Search'}
            </button>
          </div>

          <div className="flex-1 overflow-y-auto space-y-2">
            {memoryResults === null && (
              <p className="text-gray-500 text-xs text-center mt-8">
                Search your Krnl-AI memory to find relevant context
              </p>
            )}
            {memoryResults?.length === 0 && (
              <p className="text-gray-500 text-xs text-center mt-8">No results found</p>
            )}
            {memoryResults?.map((hit) => (
              <div key={hit.id} className="bg-gray-800 rounded-lg p-2">
                <div className="flex justify-between items-start mb-1">
                  <span className="text-[10px] text-krnl-400">{hit.source || 'memory'}</span>
                  <span className="text-[10px] text-gray-500">
                    {(hit.score * 100).toFixed(0)}%
                  </span>
                </div>
                <p className="text-xs text-gray-300 line-clamp-3">{hit.content}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
