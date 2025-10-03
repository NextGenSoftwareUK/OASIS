import React, { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface KarmaManagementProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  showLeaderboard?: boolean;
  showHistory?: boolean;
  customStyles?: React.CSSProperties;
}

export const KarmaManagement: React.FC<KarmaManagementProps> = ({
  avatarId,
  theme = 'dark',
  showLeaderboard = true,
  showHistory = true,
  customStyles = {}
}) => {
  const client = new OASISClient();

  const { data: karma, isLoading } = useQuery(
    ['karma', avatarId],
    () => client.getAvatarKarma(avatarId)
  );

  const { data: history } = useQuery(
    ['karma-history', avatarId],
    () => client.getKarmaHistory(avatarId),
    { enabled: showHistory }
  );

  return (
    <div className={`oasis-karma oasis-karma--${theme}`} style={customStyles}>
      <div className="oasis-karma__current">
        <h3>Karma Points</h3>
        {isLoading ? (
          <div className="oasis-karma__loading">Loading...</div>
        ) : (
          <div className="oasis-karma__value">{karma?.total || 0}</div>
        )}
      </div>

      {showHistory && history && (
        <div className="oasis-karma__history">
          <h4>Recent Activity</h4>
          <ul>
            {history.map((entry: any) => (
              <li key={entry.id}>
                <span className="karma-amount">{entry.amount > 0 ? '+' : ''}{entry.amount}</span>
                <span className="karma-reason">{entry.reason}</span>
                <span className="karma-date">{new Date(entry.date).toLocaleDateString()}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="oasis-karma__stats">
        <div className="stat">
          <span className="label">Rank</span>
          <span className="value">#{karma?.rank || '-'}</span>
        </div>
        <div className="stat">
          <span className="label">Level</span>
          <span className="value">{karma?.level || 1}</span>
        </div>
        <div className="stat">
          <span className="label">Next Level</span>
          <span className="value">{karma?.nextLevelAt || '-'}</span>
        </div>
      </div>
    </div>
  );
};

export default KarmaManagement;

