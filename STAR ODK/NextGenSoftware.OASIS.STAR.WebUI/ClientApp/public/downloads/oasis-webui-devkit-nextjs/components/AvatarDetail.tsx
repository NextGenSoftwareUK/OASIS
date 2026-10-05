import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

interface AvatarDetailProps {
  avatarId: string;
}

export default function AvatarDetail({ avatarId }: AvatarDetailProps) {
  const { client } = useOASIS();
  const [avatar, setAvatar] = useState<any>(null);

  useEffect(() => {
    loadAvatar();
  }, [avatarId]);

  async function loadAvatar() {
    const response = await client.get(`/api/avatar/${avatarId}`);
    setAvatar(response.data);
  }

  if (!avatar) return <div>Loading...</div>;

  return (
    <div className="avatar-detail">
      <div className="header">
        <div className="avatar"></div>
        <div>
          <h2>{avatar.username}</h2>
          <p>{avatar.email}</p>
          <p>Level {avatar.level}</p>
        </div>
      </div>
      <div className="stats">
        <div className="stat"><div className="value">{avatar.karma}</div><div>Karma</div></div>
        <div className="stat"><div className="value">{avatar.nftCount || 0}</div><div>NFTs</div></div>
      </div>
      <style jsx>{`
        .avatar-detail { background: white; padding: 24px; border-radius: 12px; }
        .header { display: flex; gap: 20px; margin-bottom: 20px; }
        .avatar { width: 100px; height: 100px; border-radius: 50%; background: linear-gradient(135deg, #667eea, #764ba2); }
        .stats { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; }
        .stat { text-align: center; padding: 16px; background: #f5f5f5; border-radius: 8px; }
        .value { font-size: 24px; font-weight: 600; color: #667eea; }
      `}</style>
    </div>
  );
}



