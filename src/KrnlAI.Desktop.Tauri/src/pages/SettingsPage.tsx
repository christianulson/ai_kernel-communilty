import { useState } from 'react';
import { apiGet } from '../api/client';

export default function SettingsPage() {
  const [endpoint, setEndpoint] = useState('http://localhost:5235');

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>⚙️ Configurações</h1>
      <p style={{ color: '#8AA0BC', marginBottom: 20 }}>Configure sua conexão com o servidor Krnl-AI.</p>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Endpoint da API</h3>
        <input value={endpoint} onChange={e => setEndpoint(e.target.value)} placeholder="http://localhost:5235" />
        <p style={{ color: '#8AA0BC', fontSize: 12, marginTop: 8 }}>
          Endereço do servidor Krnl-AI Gateway.
        </p>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Sobre</h3>
        <p style={{ color: '#8AA0BC' }}>
          Krnl-AI Desktop v0.1.0 (Tauri)<br />
          Cross-platform desktop client for Krnl-AI cognitive agent.
        </p>
      </div>
    </div>
  );
}
