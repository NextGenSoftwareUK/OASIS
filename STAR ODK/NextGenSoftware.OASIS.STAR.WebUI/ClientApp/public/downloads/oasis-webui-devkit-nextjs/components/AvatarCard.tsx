interface AvatarCardProps {
  username: string;
  karma: number;
  level: number;
}

export default function AvatarCard({ username, karma, level }: AvatarCardProps) {
  return (
    <div className="avatar-card">
      <div className="avatar"></div>
      <h3>{username}</h3>
      <div className="stats">
        <div className="stat"><div className="value">{karma}</div><div>Karma</div></div>
        <div className="stat"><div className="value">{level}</div><div>Level</div></div>
      </div>
      <style jsx>{`
        .avatar-card { background: white; padding: 20px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); text-align: center; }
        .avatar { width: 80px; height: 80px; border-radius: 50%; background: linear-gradient(135deg, #667eea, #764ba2); margin: 0 auto 16px; }
        .stats { display: flex; justify-content: space-around; margin-top: 16px; }
        .stat { text-align: center; }
        .value { font-size: 20px; font-weight: 600; color: #667eea; }
      `}</style>
    </div>
  );
}



