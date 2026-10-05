import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

interface AvatarDetailServerProps {
  avatarId: string;
  theme?: 'light' | 'dark';
}

export default async function AvatarDetailServer({ 
  avatarId, 
  theme = 'dark' 
}: AvatarDetailServerProps) {
  const client = new OASISServerClient();
  const avatar = await client.getAvatarDetail(avatarId);

  return (
    <div className={`oasis-avatar-detail oasis-avatar-detail--${theme}`}>
      <div className="avatar-header">
        <img 
          src={avatar.image || '/default-avatar.png'} 
          alt={avatar.username}
          className="avatar-image" 
        />
        <div className="avatar-info">
          <h2>{avatar.username}</h2>
          <p className="email">{avatar.email}</p>
        </div>
      </div>

      <div className="avatar-details">
        <div className="detail-row">
          <span className="label">Full Name:</span>
          <span className="value">{avatar.firstName} {avatar.lastName}</span>
        </div>
        <div className="detail-row">
          <span className="label">Bio:</span>
          <span className="value">{avatar.bio || 'No bio provided'}</span>
        </div>
        <div className="detail-row">
          <span className="label">Member Since:</span>
          <span className="value">
            {new Date(avatar.createdDate).toLocaleDateString()}
          </span>
        </div>
        <div className="detail-row">
          <span className="label">Karma:</span>
          <span className="value karma">{avatar.karma || 0}</span>
        </div>
        <div className="detail-row">
          <span className="label">Level:</span>
          <span className="value">{avatar.level || 1}</span>
        </div>
      </div>

      <style jsx>{`
        .oasis-avatar-detail {
          padding: 1.5rem;
          background: ${theme === 'dark' ? '#1a1a1a' : 'white'};
          color: ${theme === 'dark' ? 'white' : '#333'};
          border-radius: 12px;
        }
        .avatar-header {
          display: flex;
          align-items: center;
          gap: 1rem;
          margin-bottom: 1.5rem;
        }
        .avatar-image {
          width: 80px;
          height: 80px;
          border-radius: 50%;
        }
        .detail-row {
          display: flex;
          justify-content: space-between;
          padding: 0.5rem 0;
          border-bottom: 1px solid ${theme === 'dark' ? '#2a2a2a' : '#f5f5f5'};
        }
        .karma {
          color: #00bcd4;
          font-weight: bold;
        }
      `}</style>
    </div>
  );
}

