import React from 'react';

export interface AvatarCardProps {
  avatar: {
    id: string;
    username: string;
    image?: string;
    karma?: number;
    level?: number;
  };
  theme?: 'light' | 'dark';
  onClick?: (avatar: any) => void;
  showKarma?: boolean;
  showLevel?: boolean;
  customStyles?: React.CSSProperties;
}

export const AvatarCard: React.FC<AvatarCardProps> = ({
  avatar,
  theme = 'dark',
  onClick,
  showKarma = true,
  showLevel = true,
  customStyles = {}
}) => {
  return (
    <div 
      className={`oasis-avatar-card oasis-avatar-card--${theme}`}
      style={customStyles}
      onClick={() => onClick?.(avatar)}
    >
      <div className="avatar-card__image">
        <img src={avatar.image || '/default-avatar.png'} alt={avatar.username} />
      </div>
      <div className="avatar-card__info">
        <h4 className="username">{avatar.username}</h4>
        {showKarma && (
          <div className="stat">
            <span className="label">Karma:</span>
            <span className="value">{avatar.karma || 0}</span>
          </div>
        )}
        {showLevel && (
          <div className="stat">
            <span className="label">Level:</span>
            <span className="value">{avatar.level || 1}</span>
          </div>
        )}
      </div>
    </div>
  );
};

export default AvatarCard;

