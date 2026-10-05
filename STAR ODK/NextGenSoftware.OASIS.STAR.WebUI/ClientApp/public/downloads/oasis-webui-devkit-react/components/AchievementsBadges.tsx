import React from 'react';
import { useQuery } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface AchievementsBadgesProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  layout?: 'grid' | 'list';
  showProgress?: boolean;
  customStyles?: React.CSSProperties;
}

export const AchievementsBadges: React.FC<AchievementsBadgesProps> = ({
  avatarId,
  theme = 'dark',
  layout = 'grid',
  showProgress = true,
  customStyles = {}
}) => {
  const client = new OASISClient();

  const { data: achievements, isLoading } = useQuery(
    ['achievements', avatarId],
    () => client.getAchievements(avatarId)
  );

  return (
    <div className={`oasis-achievements oasis-achievements--${theme} oasis-achievements--${layout}`} style={customStyles}>
      <div className="achievements-header">
        <h3>Achievements & Badges</h3>
        <span className="count">
          {achievements?.filter((a: any) => a.unlocked).length || 0} / {achievements?.length || 0}
        </span>
      </div>

      <div className={`achievements-${layout}`}>
        {isLoading ? (
          <div className="loading">Loading achievements...</div>
        ) : (
          achievements?.map((achievement: any) => (
            <div 
              key={achievement.id}
              className={`achievement-item ${achievement.unlocked ? 'unlocked' : 'locked'}`}
            >
              <div className="achievement-icon">
                {achievement.unlocked ? (
                  <img src={achievement.iconUrl} alt={achievement.name} />
                ) : (
                  <div className="locked-icon">🔒</div>
                )}
              </div>
              <div className="achievement-info">
                <h4>{achievement.name}</h4>
                <p>{achievement.description}</p>
                {showProgress && !achievement.unlocked && achievement.progress && (
                  <div className="progress-bar">
                    <div 
                      className="progress-fill"
                      style={{ width: `${(achievement.progress.current / achievement.progress.total) * 100}%` }}
                    />
                    <span className="progress-text">
                      {achievement.progress.current} / {achievement.progress.total}
                    </span>
                  </div>
                )}
                {achievement.unlocked && (
                  <span className="unlock-date">
                    Unlocked: {new Date(achievement.unlockedAt).toLocaleDateString()}
                  </span>
                )}
              </div>
              {achievement.rarity && (
                <span className={`rarity rarity--${achievement.rarity}`}>
                  {achievement.rarity}
                </span>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default AchievementsBadges;

