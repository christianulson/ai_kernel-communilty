import { useState } from 'react';
import { apiGet, apiPost } from '../api/client';

export default function ApiKeysPage() {
  const [keyName, setKeyName] = useState('');
  const [createdKey, setCreatedKey] = useState('');

  const createKey = async () => {
    if (!keyName.trim()) return;
    const result = await apiPost<{fullKey: string}>('/account/api-keys', {
      name: keyName.trim(),
      scope: 0,
    });
    setCreatedKey(result?.fullKey || 'Falha ao criar chave.');
  };

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>🔑 API Keys</h1>
      <p style={{ color: '#8AA0BC', marginBottom: 20 }}>Gerencie suas chaves de API.</p>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Nova Chave</h3>
        <input value={keyName} onChange={e => setKeyName(e.target.value)} placeholder="Nome da chave" style={{ marginBottom: 12 }} />
        <button onClick={createKey} style={{ background: '#38BDF8', color: '#03111D' }}>Criar Chave</button>
        {createdKey && (
          <p style={{ marginTop: 12, padding: 10, background: '#1A2E3F', borderRadius: 8, wordBreak: 'break-all' }}>
            {createdKey}
          </p>
        )}
      </div>
    </div>
  );
}
