import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function AchievementsBadges() {
  const { client } = useOASIS();
  const [achievements, setAchievements] = useState([]);
  const [activeTab, setActiveTab] = useState('all');

  useEffect(() => {
    loadAchievements();
  }, []);

  async function loadAchievements() {
    const response = await client.get('/api/achievements');
    setAchievements(response.data);
  }

  const filtered = activeTab === 'all' ? achievements : activeTab === 'unlocked' ? achievements.filter((a: any) => a.unlocked) : achievements.filter((a: any) => !a.unlocked);

  return (
    <div className="achievements">
      <h2>Achievements & Badges</h2>
      <div className="tabs">
        <button className={activeTab === 'all' ? 'active' : ''} onClick={() => setActiveTab('all')}>All</button>
        <button className={activeTab === 'unlocked' ? 'active' : ''} onClick={() => setActiveTab('unlocked')}>Unlocked</button>
        <button className={activeTab === 'locked' ? 'active' : ''} onClick={() => setActiveTab('locked')}>Locked</button>
      </div>
      <div className="grid">
        {filtered.map((a: any) => (
          <div key={a.id} className={`achievement ${a.unlocked ? '' : 'locked'}`}>
            <div className="icon">{a.icon}{a.unlocked && <span className="badge">✓</span>}</div>
            <div><h3>{a.name}</h3><p>{a.description}</p></div>
          </div>
        ))}
      </div>
      <style jsx>{`
        .tabs { display: flex; gap: 8px; margin-bottom: 24px; }
        .tabs button { padding: 8px 16px; border: 1px solid #ddd; background: white; border-radius: 20px; cursor: pointer; }
        .tabs button.active { background: #4A90E2; color: white; }
        .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 20px; }
        .achievement { display: flex; gap: 16px; background: white; padding: 20px; border-radius: 12px; }
        .achievement.locked { opacity: 0.6; }
        .icon { font-size: 48px; position: relative; }
        .badge { position: absolute; top: -4px; right: -4px; width: 20px; height: 20px; background: #27ae60; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 12px; }
      `}</style>
    </div>
  );
}



