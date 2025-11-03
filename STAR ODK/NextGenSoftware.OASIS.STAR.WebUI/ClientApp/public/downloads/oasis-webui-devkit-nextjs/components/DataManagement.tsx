import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function DataManagement() {
  const { client } = useOASIS();
  const [holons, setHolons] = useState([]);
  const [name, setName] = useState('');
  const [holonType, setHolonType] = useState('');

  useEffect(() => {
    loadHolons();
  }, []);

  async function loadHolons() {
    const response = await client.get('/api/data/holons');
    setHolons(response.data);
  }

  async function createHolon() {
    await client.post('/api/data/holons', { name, holonType });
    setName('');
    setHolonType('');
    loadHolons();
  }

  return (
    <div className="data-management">
      <div className="create-form">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Holon Name" />
        <input value={holonType} onChange={(e) => setHolonType(e.target.value)} placeholder="Type" />
        <button onClick={createHolon}>Create</button>
      </div>
      {holons.map((holon: any) => (
        <div key={holon.id} className="holon-card">
          <h3>{holon.name}</h3>
          <p>Type: {holon.holonType}</p>
        </div>
      ))}
      <style jsx>{`
        .create-form { display: flex; gap: 8px; margin-bottom: 20px; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 10px 20px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
        .holon-card { background: white; padding: 16px; margin-bottom: 12px; border-radius: 8px; }
      `}</style>
    </div>
  );
}



