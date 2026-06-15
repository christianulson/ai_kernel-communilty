import { useEffect, useState } from 'react';
import { apiGet } from '../api/client';

export default function DashboardPage() {
  const [health, setHealth] = useState<string>('Verificando...');

  useEffect(() => {
    apiGet<{status: string}>('/health').then(r => setHealth(r?.status || 'Offline'));
  }, []);

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>📊 Dashboard</h1>
      <p style={{ color: '#8AA0BC', marginBottom: 20 }}>Status do sistema Krnl-AI.</p>

      <div className="card">
        <h3>Status do Servidor</h3>
        <p style={{ fontSize: 28, fontWeight: 700, color: health === 'healthy' ? '#22C55E' : '#FB7185', marginTop: 8 }}>
          {health === 'healthy' ? '🟢 Online' : '🔴 Offline'}
        </p>
      </div>

      <div className="card">
        <h3>Informações</h3>
        <p style={{ color: '#8AA0BC', marginTop: 8 }}>
          Esta é a versão Tauri (cross-platform) do Krnl-AI Desktop.
          Conecte-se a um servidor Krnl-AI para funcionalidade completa.
        </p>
      </div>
    </div>
  );
}
