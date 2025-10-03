import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function KarmaLeaderboard() {
  const { client } = useOASIS();
  const [leaderboard, setLeaderboard] = useState([]);

  useEffect(() => {
    loadLeaderboard();
  }, []);

  async function loadLeaderboard() {
    const response = await client.get('/api/karma/leaderboard');
    setLeaderboard(response.data);
  }

  return (
    <div className="oasis-karma-leaderboard">
      <h2>Karma Leaderboard</h2>
      {leaderboard.map((leader: any, i) => (
        <div key={i} className="leader">
          <div className={`rank ${i === 0 ? 'gold' : i === 1 ? 'silver' : i === 2 ? 'bronze' : ''}`}>{i + 1}</div>
          <div className="avatar">{leader.username.charAt(0)}</div>
          <div className="info">
            <strong>{leader.username}</strong>
            <small>Level {leader.level}</small>
          </div>
          <div className="karma">{leader.karma.toLocaleString()}</div>
        </div>
      ))}
      <style jsx>{`
        .leader { display: flex; align-items: center; gap: 16px; padding: 12px; border-bottom: 1px solid #eee; }
        .rank { font-size: 24px; font-weight: 600; width: 40px; }
        .rank.gold { color: #f39c12; }
        .rank.silver { color: #95a5a6; }
        .rank.bronze { color: #cd7f32; }
        .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; display: flex; align-items: center; justify-content: center; font-weight: 600; }
        .info { flex: 1; }
        .karma { font-size: 18px; font-weight: 600; color: #667eea; }
      `}</style>
    </div>
  );
}



