import { Routes, Route, NavLink } from 'react-router-dom';
import ChatPage from './pages/ChatPage';
import DashboardPage from './pages/DashboardPage';
import ApiKeysPage from './pages/ApiKeysPage';
import SettingsPage from './pages/SettingsPage';
import './App.css';

function App() {
  return (
    <div style={{ display: 'flex', height: '100vh' }}>
      <nav style={{ width: 220, background: '#0E1727', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
        <h2 style={{ margin: '0 0 16px', fontSize: 18, fontWeight: 700, color: '#E5EEFC' }}>⚡ Krnl-AI</h2>
        {[
          ['💬 Chat', '/'],
          ['📊 Dashboard', '/dashboard'],
          ['🔑 API Keys', '/api-keys'],
          ['⚙️ Settings', '/settings'],
        ].map(([label, path]) => (
          <NavLink key={path} to={path} style={({ isActive }) => ({
            padding: '10px 14px', borderRadius: 10, textDecoration: 'none', fontSize: 14,
            color: isActive ? '#E5EEFC' : '#8AA0BC', background: isActive ? '#1E293B' : 'transparent',
          })}>{label}</NavLink>
        ))}
      </nav>
      <main style={{ flex: 1, padding: 24, overflow: 'auto', background: '#08111F', color: '#E5EEFC' }}>
        <Routes>
          <Route path="/" element={<ChatPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/api-keys" element={<ApiKeysPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
