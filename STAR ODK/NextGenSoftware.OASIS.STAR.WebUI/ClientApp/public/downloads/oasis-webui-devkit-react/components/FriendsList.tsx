import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface FriendsListProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  showOnlineStatus?: boolean;
  enableManagement?: boolean;
  customStyles?: React.CSSProperties;
}

export const FriendsList: React.FC<FriendsListProps> = ({
  avatarId,
  theme = 'dark',
  showOnlineStatus = true,
  enableManagement = true,
  customStyles = {}
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const queryClient = useQueryClient();
  const client = new OASISClient();

  const { data: friends, isLoading } = useQuery(
    ['friends', avatarId],
    () => client.getFriends(avatarId)
  );

  const removeMutation = useMutation(
    (friendId: string) => client.removeFriend(avatarId, friendId),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['friends']);
      }
    }
  );

  const filteredFriends = friends?.filter((friend: any) =>
    friend.username.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className={`oasis-friends-list oasis-friends-list--${theme}`} style={customStyles}>
      <div className="friends-header">
        <h3>Friends ({friends?.length || 0})</h3>
        <input
          type="text"
          placeholder="Search friends..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      <div className="friends-list">
        {isLoading ? (
          <div className="loading">Loading friends...</div>
        ) : (
          filteredFriends?.map((friend: any) => (
            <div key={friend.id} className="friend-item">
              <img src={friend.image || '/default-avatar.png'} alt={friend.username} />
              <div className="friend-info">
                <h4>{friend.username}</h4>
                {showOnlineStatus && (
                  <span className={`status ${friend.isOnline ? 'online' : 'offline'}`}>
                    {friend.isOnline ? '🟢 Online' : '⚫ Offline'}
                  </span>
                )}
                <p className="karma">Karma: {friend.karma || 0}</p>
              </div>
              {enableManagement && (
                <div className="friend-actions">
                  <button className="message-btn">💬</button>
                  <button 
                    className="remove-btn"
                    onClick={() => removeMutation.mutate(friend.id)}
                  >
                    ✕
                  </button>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default FriendsList;

