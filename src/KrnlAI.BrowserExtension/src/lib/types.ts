export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: number;
}

export interface PageContext {
  title: string;
  url: string;
  text: string;
  metaDescription?: string;
  selection?: string;
}

export interface KrnlClientConfig {
  endpoint: string;
  maxRetries?: number;
  timeoutMs?: number;
}

export interface HealthStatus {
  ok: boolean;
  ts: string;
}

export interface CommandResponse {
  narration?: string;
  error?: string;
}

export interface MemoryHit {
  id: string;
  content: string;
  source: string;
  score: number;
}

export interface MemorySearchResult {
  hits: MemoryHit[];
  totalCount: number;
}

export interface SlashCommandDef {
  id: string;
  label: string;
  description: string;
}

export const SLASH_COMMANDS: SlashCommandDef[] = [
  { id: '/resume', label: '/resume', description: 'Resume/Summarize current page' },
  { id: '/translate', label: '/translate', description: 'Translate page content' },
  { id: '/extract', label: '/extract', description: 'Extract page to memory' },
  { id: '/ask', label: '/ask', description: 'Ask a question about the page' },
];

export const DEFAULT_ENDPOINT = 'http://localhost:5100';
