import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function ProviderManagement() {
  const { client } = useOASIS();
  const [providers, setProviders] = useState([]);

  useEffect(() => {
    loadProviders();
  }, []);

  async function loadProviders() {
    const response = await client.get('/api/providers');
    setProviders(response.data);
  }

  async function toggleProvider(id: string) {
    await client.post(`/api/providers/${id}/toggle`);
    loadProviders();
  }

  return (
    <div className="provider-management">
      {providers.map((provider: any) => (
        <div key={provider.id} className="provider-card">
          <div>
            <h3>{provider.name}</h3>
            <span className={`status ${provider.isActive ? 'active' : 'inactive'}`}>{provider.isActive ? 'Active' : 'Inactive'}</span>
          </div>
          <button onClick={() => toggleProvider(provider.id)}>Toggle</button>
        </div>
      ))}
      <style jsx>{`
        .provider-card { display: flex; justify-content: space-between; align-items: center; padding: 16px; background: white; margin-bottom: 12px; border-radius: 8px; }
        .status { padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; }
        .status.active { background: #d4edda; color: #155724; }
        .status.inactive { background: #f8d7da; color: #721c24; }
        button { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; background: #4A90E2; color: white; }
      `}</style>
    </div>
  );
}



