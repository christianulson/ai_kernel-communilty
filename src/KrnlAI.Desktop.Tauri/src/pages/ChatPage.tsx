import { useState } from 'react';
import { apiPost } from '../api/client';

export default function ChatPage() {
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<{role: string; content: string}[]>([]);
  const [loading, setLoading] = useState(false);

  const sendMessage = async () => {
    if (!input.trim()) return;
    const userMsg = { role: 'user', content: input };
    setMessages(prev => [...prev, userMsg]);
    setInput('');
    setLoading(true);

    const result = await apiPost<{narration: string}>('/agent/run', {
      prompt: input,
    });

    setMessages(prev => [...prev, {
      role: 'assistant',
      content: result?.narration || 'Sem resposta do servidor.',
    }]);
    setLoading(false);
  };

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>💬 Chat</h1>
      <p style={{ color: '#8AA0BC', marginBottom: 20 }}>Converse com o agente Krnl-AI.</p>

      <div className="card" style={{ height: 400, overflow: 'auto', marginBottom: 16 }}>
        {messages.map((msg, i) => (
          <div key={i} style={{
            marginBottom: 12, padding: 12, borderRadius: 10,
            background: msg.role === 'user' ? '#1E3A5F' : '#1A2E3F',
          }}>
            <strong>{msg.role === 'user' ? '👤 Você' : '🤖 Krnl-AI'}</strong>
            <p style={{ marginTop: 4, lineHeight: 1.5 }}>{msg.content}</p>
          </div>
        ))}
        {loading && <p style={{ color: '#8AA0BC' }}>Processando...</p>}
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        <textarea
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && !e.shiftKey && (e.preventDefault(), sendMessage())}
          placeholder="Digite sua mensagem..."
          rows={3}
          style={{ flex: 1, resize: 'none' }}
        />
        <button onClick={sendMessage} disabled={loading}
          style={{ background: '#38BDF8', color: '#03111D', padding: '10px 24px', alignSelf: 'flex-end' }}>
          Enviar
        </button>
      </div>
    </div>
  );
}
