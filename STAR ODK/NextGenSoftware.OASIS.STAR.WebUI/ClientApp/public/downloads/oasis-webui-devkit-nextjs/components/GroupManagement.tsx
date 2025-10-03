import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function GroupManagement() {
  const { client } = useOASIS();
  const [groups, setGroups] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [newGroup, setNewGroup] = useState({ name: '', description: '', emoji: '' });

  useEffect(() => {
    loadGroups();
  }, []);

  async function loadGroups() {
    const response = await client.get('/api/groups');
    setGroups(response.data);
  }

  async function createGroup() {
    await client.post('/api/groups', newGroup);
    setShowModal(false);
    setNewGroup({ name: '', description: '', emoji: '' });
    loadGroups();
  }

  return (
    <div className="group-management">
      <div className="header">
        <h2>My Groups</h2>
        <button onClick={() => setShowModal(true)}>Create Group</button>
      </div>
      <div className="grid">
        {groups.map((group: any) => (
          <div key={group.id} className="group-card">
            <div className="emoji">{group.emoji || '👥'}</div>
            <h3>{group.name}</h3>
            <p>{group.description}</p>
          </div>
        ))}
      </div>
      {showModal && (
        <div className="modal" onClick={() => setShowModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Create New Group</h3>
            <input value={newGroup.name} onChange={(e) => setNewGroup({...newGroup, name: e.target.value})} placeholder="Group Name" />
            <textarea value={newGroup.description} onChange={(e) => setNewGroup({...newGroup, description: e.target.value})} placeholder="Description"></textarea>
            <button onClick={createGroup}>Create</button>
          </div>
        </div>
      )}
      <style jsx>{`
        .header { display: flex; justify-content: space-between; margin-bottom: 24px; }
        .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; }
        .group-card { background: white; padding: 20px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        .emoji { font-size: 48px; }
        .modal { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
        .modal-content { background: white; padding: 24px; border-radius: 12px; max-width: 500px; width: 90%; }
      `}</style>
    </div>
  );
}



