import type { ChatMessage } from './types';
import { DEFAULT_ENDPOINT } from './types';

const KEYS = {
  endpoint: 'krnlai_endpoint',
  chatHistory: 'krnlai_chat_history',
  sidebarChatHistory: 'krnlai_sidebar_chat_history',
};

export async function getEndpoint(): Promise<string> {
  const result = await chrome.storage.sync.get(KEYS.endpoint);
  return (result[KEYS.endpoint] as string) || DEFAULT_ENDPOINT;
}

export async function setEndpoint(url: string): Promise<void> {
  await chrome.storage.sync.set({ [KEYS.endpoint]: url });
}

export async function getChatHistory(key = 'popup'): Promise<ChatMessage[]> {
  const storageKey = key === 'sidebar' ? KEYS.sidebarChatHistory : KEYS.chatHistory;
  const result = await chrome.storage.local.get(storageKey);
  return (result[storageKey] as ChatMessage[]) || [];
}

export async function saveChatHistory(messages: ChatMessage[], key = 'popup'): Promise<void> {
  const storageKey = key === 'sidebar' ? KEYS.sidebarChatHistory : KEYS.chatHistory;
  const trimmed = messages.slice(-100);
  await chrome.storage.local.set({ [storageKey]: trimmed });
}

export async function clearChatHistory(key = 'popup'): Promise<void> {
  const storageKey = key === 'sidebar' ? KEYS.sidebarChatHistory : KEYS.chatHistory;
  await chrome.storage.local.remove(storageKey);
}
