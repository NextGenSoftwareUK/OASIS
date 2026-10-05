import React, { useState } from 'react';
import { useQuery, useMutation } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface AvatarDetailProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  editable?: boolean;
  onUpdate?: (avatar: any) => void;
  customStyles?: React.CSSProperties;
}

export const AvatarDetail: React.FC<AvatarDetailProps> = ({
  avatarId,
  theme = 'dark',
  editable = true,
  onUpdate,
  customStyles = {}
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState<any>({});
  const client = new OASISClient();

  const { data: avatar, isLoading, refetch } = useQuery(
    ['avatar-detail', avatarId],
    () => client.getAvatarDetail(avatarId)
  );

  const updateMutation = useMutation(
    (data: any) => client.updateAvatar(avatarId, data),
    {
      onSuccess: (updated) => {
        onUpdate?.(updated);
        refetch();
        setIsEditing(false);
      }
    }
  );

  const handleSave = () => {
    updateMutation.mutate(formData);
  };

  if (isLoading) return <div className="oasis-avatar-detail__loading">Loading...</div>;

  return (
    <div className={`oasis-avatar-detail oasis-avatar-detail--${theme}`} style={customStyles}>
      <div className="avatar-header">
        <img src={avatar?.image || '/default-avatar.png'} alt={avatar?.username} className="avatar-image" />
        <div className="avatar-info">
          <h2>{avatar?.username}</h2>
          <p className="email">{avatar?.email}</p>
        </div>
        {editable && (
          <button onClick={() => setIsEditing(!isEditing)} className="edit-btn">
            {isEditing ? 'Cancel' : 'Edit'}
          </button>
        )}
      </div>

      <div className="avatar-details">
        {isEditing ? (
          <div className="edit-form">
            <input
              type="text"
              placeholder="First Name"
              value={formData.firstName || avatar?.firstName}
              onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
            />
            <input
              type="text"
              placeholder="Last Name"
              value={formData.lastName || avatar?.lastName}
              onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
            />
            <textarea
              placeholder="Bio"
              value={formData.bio || avatar?.bio}
              onChange={(e) => setFormData({ ...formData, bio: e.target.value })}
            />
            <button onClick={handleSave} disabled={updateMutation.isLoading}>
              {updateMutation.isLoading ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        ) : (
          <>
            <div className="detail-row">
              <span className="label">Full Name:</span>
              <span className="value">{avatar?.firstName} {avatar?.lastName}</span>
            </div>
            <div className="detail-row">
              <span className="label">Bio:</span>
              <span className="value">{avatar?.bio || 'No bio provided'}</span>
            </div>
            <div className="detail-row">
              <span className="label">Member Since:</span>
              <span className="value">{new Date(avatar?.createdDate).toLocaleDateString()}</span>
            </div>
            <div className="detail-row">
              <span className="label">Karma:</span>
              <span className="value karma">{avatar?.karma || 0}</span>
            </div>
            <div className="detail-row">
              <span className="label">Level:</span>
              <span className="value">{avatar?.level || 1}</span>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default AvatarDetail;

