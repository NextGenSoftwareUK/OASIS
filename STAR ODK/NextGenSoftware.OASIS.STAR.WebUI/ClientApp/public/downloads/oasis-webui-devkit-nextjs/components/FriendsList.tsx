import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function FriendsList() {
  const { client } = useOASIS();
  const [friends, setFriends] = useState([]);

  useEffect(() => {
    loadFriends();
  }, []);

  async function loadFriends() {
    const response = await client.get('/api/friends');
    setFriends(response.data);
  }

  return (
    <div className="friends-list">
      {friends.map((friend: any) => (
        <div key={friend.id} className="friend">
          <div className="avatar"></div>
          <div className="info"><strong>{friend.username}</strong><br/><small>Level {friend.level}</small></div>
          <div className={`status ${friend.online ? 'online' : 'offline'}`}></div>
        </div>
      ))}
      <style jsx>{`
        .friend { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; align-items: center; }
        .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; }
        .info { flex: 1; }
        .status { width: 12px; height: 12px; border-radius: 50%; }
        .status.online { background: #27ae60; }
        .status.offline { background: #95a5a6; }
      `}</style>
    </div>
  );
}



