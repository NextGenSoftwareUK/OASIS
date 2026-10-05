import React from 'react';
import { useQuery } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface KarmaLeaderboardProps {
  limit?: number;
  timeRange?: 'day' | 'week' | 'month' | 'all';
  highlightCurrentUser?: boolean;
  currentAvatarId?: string;
  theme?: 'light' | 'dark';
  customStyles?: React.CSSProperties;
}

export const KarmaLeaderboard: React.FC<KarmaLeaderboardProps> = ({
  limit = 10,
  timeRange = 'week',
  highlightCurrentUser = true,
  currentAvatarId,
  theme = 'dark',
  customStyles = {}
}) => {
  const client = new OASISClient();

  const { data: leaderboard, isLoading } = useQuery(
    ['karma-leaderboard', timeRange, limit],
    () => client.getKarmaLeaderboard(timeRange, limit)
  );

  const getMedalEmoji = (rank: number) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  };

  return (
    <div className={`oasis-karma-leaderboard oasis-karma-leaderboard--${theme}`} style={customStyles}>
      <div className="leaderboard-header">
        <h3>Karma Leaderboard</h3>
        <span className="time-range">{timeRange.charAt(0).toUpperCase() + timeRange.slice(1)}</span>
      </div>

      <div className="leaderboard-list">
        {isLoading ? (
          <div className="loading">Loading leaderboard...</div>
        ) : (
          leaderboard?.map((entry: any, index: number) => (
            <div 
              key={entry.avatarId}
              className={`leaderboard-item ${
                highlightCurrentUser && entry.avatarId === currentAvatarId ? 'current-user' : ''
              }`}
            >
              <span className="rank">{getMedalEmoji(index + 1)}</span>
              <img 
                src={entry.avatarImage || '/default-avatar.png'} 
                alt={entry.username}
                className="avatar"
              />
              <div className="user-info">
                <h4>{entry.username}</h4>
                <span className="level">Level {entry.level}</span>
              </div>
              <div className="karma-score">
                <span className="value">{entry.karma.toLocaleString()}</span>
                <span className="label">karma</span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default KarmaLeaderboard;

