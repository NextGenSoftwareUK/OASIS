import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface GroupManagementProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  enableCreation?: boolean;
  customStyles?: React.CSSProperties;
}

export const GroupManagement: React.FC<GroupManagementProps> = ({
  avatarId,
  theme = 'dark',
  enableCreation = true,
  customStyles = {}
}) => {
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newGroup, setNewGroup] = useState({ name: '', description: '', isPrivate: false });
  const queryClient = useQueryClient();
  const client = new OASISClient();

  const { data: groups, isLoading } = useQuery(
    ['groups', avatarId],
    () => client.getGroups(avatarId)
  );

  const createMutation = useMutation(
    (data: any) => client.createGroup(avatarId, data),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['groups']);
        setShowCreateDialog(false);
        setNewGroup({ name: '', description: '', isPrivate: false });
      }
    }
  );

  const leaveMutation = useMutation(
    (groupId: string) => client.leaveGroup(avatarId, groupId),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['groups']);
      }
    }
  );

  const handleCreate = () => {
    createMutation.mutate(newGroup);
  };

  return (
    <div className={`oasis-group-mgmt oasis-group-mgmt--${theme}`} style={customStyles}>
      <div className="groups-header">
        <h3>My Groups</h3>
        {enableCreation && (
          <button onClick={() => setShowCreateDialog(true)}>+ Create Group</button>
        )}
      </div>

      {showCreateDialog && (
        <div className="create-dialog">
          <input
            type="text"
            placeholder="Group Name"
            value={newGroup.name}
            onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })}
          />
          <textarea
            placeholder="Description"
            value={newGroup.description}
            onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })}
          />
          <label>
            <input
              type="checkbox"
              checked={newGroup.isPrivate}
              onChange={(e) => setNewGroup({ ...newGroup, isPrivate: e.target.checked })}
            />
            Private Group
          </label>
          <div className="dialog-actions">
            <button onClick={handleCreate} disabled={createMutation.isLoading}>
              Create
            </button>
            <button onClick={() => setShowCreateDialog(false)}>Cancel</button>
          </div>
        </div>
      )}

      <div className="groups-list">
        {isLoading ? (
          <div className="loading">Loading groups...</div>
        ) : (
          groups?.map((group: any) => (
            <div key={group.id} className="group-item">
              <img src={group.imageUrl || '/default-group.png'} alt={group.name} />
              <div className="group-info">
                <h4>{group.name}</h4>
                <p>{group.description}</p>
                <span className="members">👥 {group.memberCount} members</span>
              </div>
              <div className="group-actions">
                <button className="view-btn">View</button>
                <button 
                  className="leave-btn"
                  onClick={() => leaveMutation.mutate(group.id)}
                >
                  Leave
                </button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default GroupManagement;

