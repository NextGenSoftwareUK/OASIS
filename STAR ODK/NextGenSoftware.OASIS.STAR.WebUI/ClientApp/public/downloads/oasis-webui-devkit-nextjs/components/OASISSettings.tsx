import { useState } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function OASISSettings() {
  const { client } = useOASIS();
  const [theme, setTheme] = useState('light');
  const [privacy, setPrivacy] = useState('public');

  async function saveSettings() {
    await client.post('/api/settings', { theme, privacy });
  }

  return (
    <div className="oasis-settings">
      <div className="setting">
        <label>Theme</label>
        <select value={theme} onChange={(e) => setTheme(e.target.value)}>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
        </select>
      </div>
      <div className="setting">
        <label>Privacy</label>
        <select value={privacy} onChange={(e) => setPrivacy(e.target.value)}>
          <option value="public">Public</option>
          <option value="private">Private</option>
        </select>
      </div>
      <button onClick={saveSettings}>Save Settings</button>
      <style jsx>{`
        .setting { margin-bottom: 20px; }
        label { display: block; margin-bottom: 8px; font-weight: 600; }
        select { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 12px 24px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
      `}</style>
    </div>
  );
}



