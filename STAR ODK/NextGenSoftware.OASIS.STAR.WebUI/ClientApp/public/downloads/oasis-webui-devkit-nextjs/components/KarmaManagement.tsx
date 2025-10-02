'use client';

import { useState, useEffect } from 'react';
import { OASISClient } from '@oasis/api-client';

interface KarmaManagementProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  showHistory?: boolean;
}

export default function KarmaManagement({ 
  avatarId, 
  theme = 'dark',
  showHistory = true 
}: KarmaManagementProps) {
  const [karma, setKarma] = useState<any>(null);
  const [history, setHistory] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const client = new OASISClient();

  useEffect(() => {
    loadKarma();
  }, [avatarId]);

  const loadKarma = async () => {
    setLoading(true);
    try {
      const karmaData = await client.getAvatarKarma(avatarId);
      setKarma(karmaData);
      
      if (showHistory) {
        const historyData = await client.getKarmaHistory(avatarId);
        setHistory(historyData);
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading karma...</div>;
  }

  return (
    <div className={`oasis-karma-mgmt oasis-karma-mgmt--${theme}`}>
      <div className="karma-current">
        <h3>Karma Points</h3>
        <div className="karma-value">{karma?.total || 0}</div>
      </div>

      {karma && (
        <div className="karma-stats">
          <div className="stat">
            <span className="label">Rank</span>
            <span className="value">#{karma.rank || '-'}</span>
          </div>
          <div className="stat">
            <span className="label">Level</span>
            <span className="value">{karma.level || 1}</span>
          </div>
          <div className="stat">
            <span className="label">Next Level</span>
            <span className="value">{karma.nextLevelAt || '-'}</span>
          </div>
        </div>
      )}

      {showHistory && history.length > 0 && (
        <div className="karma-history">
          <h4>Recent Activity</h4>
          <ul>
            {history.map((entry: any) => (
              <li key={entry.id}>
                <span className="reason">{entry.reason}</span>
                <span className={entry.amount > 0 ? 'positive' : 'negative'}>
                  {entry.amount > 0 ? '+' : ''}{entry.amount}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <style jsx>{`
        .oasis-karma-mgmt {
          padding: 1.5rem;
          background: ${theme === 'dark' ? '#1a1a1a' : 'white'};
          color: ${theme === 'dark' ? 'white' : '#333'};
          border-radius: 12px;
        }
        .karma-current {
          text-align: center;
          margin-bottom: 1.5rem;
        }
        .karma-value {
          font-size: 3rem;
          font-weight: bold;
          color: #00bcd4;
        }
        .karma-stats {
          display: flex;
          justify-content: space-around;
          gap: 1rem;
          margin-bottom: 1.5rem;
        }
        .stat {
          text-align: center;
        }
        .stat .label {
          display: block;
          font-size: 0.875rem;
          opacity: 0.7;
        }
        .stat .value {
          font-size: 1.25rem;
          font-weight: bold;
        }
        .karma-history ul {
          list-style: none;
          padding: 0;
        }
        .karma-history li {
          display: flex;
          justify-content: space-between;
          padding: 0.5rem 0;
          border-bottom: 1px solid ${theme === 'dark' ? '#2a2a2a' : '#f5f5f5'};
        }
        .positive {
          color: #4caf50;
        }
        .negative {
          color: #f44336;
        }
      `}</style>
    </div>
  );
}

