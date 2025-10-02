import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface SocialFeedProps {
  avatarId?: string;
  feedType?: 'global' | 'friends' | 'personal';
  theme?: 'light' | 'dark';
  enablePosting?: boolean;
  customStyles?: React.CSSProperties;
}

export const SocialFeed: React.FC<SocialFeedProps> = ({
  avatarId,
  feedType = 'global',
  theme = 'dark',
  enablePosting = true,
  customStyles = {}
}) => {
  const [newPost, setNewPost] = useState('');
  const queryClient = useQueryClient();
  const client = new OASISClient();

  const { data: posts, isLoading } = useQuery(
    ['social-feed', feedType, avatarId],
    () => client.getSocialFeed(feedType, avatarId)
  );

  const postMutation = useMutation(
    (content: string) => client.createPost(avatarId, content),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['social-feed']);
        setNewPost('');
      }
    }
  );

  const likeMutation = useMutation(
    (postId: string) => client.likePost(postId),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['social-feed']);
      }
    }
  );

  const handlePost = () => {
    if (newPost.trim()) {
      postMutation.mutate(newPost);
    }
  };

  return (
    <div className={`oasis-social-feed oasis-social-feed--${theme}`} style={customStyles}>
      {enablePosting && avatarId && (
        <div className="post-composer">
          <textarea
            placeholder="What's on your mind?"
            value={newPost}
            onChange={(e) => setNewPost(e.target.value)}
            rows={3}
          />
          <button 
            onClick={handlePost}
            disabled={postMutation.isLoading || !newPost.trim()}
          >
            {postMutation.isLoading ? 'Posting...' : 'Post'}
          </button>
        </div>
      )}

      <div className="feed-posts">
        {isLoading ? (
          <div className="loading">Loading feed...</div>
        ) : (
          posts?.map((post: any) => (
            <div key={post.id} className="post-item">
              <div className="post-header">
                <img src={post.author.image} alt={post.author.username} />
                <div className="post-author">
                  <h4>{post.author.username}</h4>
                  <span className="post-time">{new Date(post.createdAt).toLocaleString()}</span>
                </div>
              </div>
              <div className="post-content">
                <p>{post.content}</p>
                {post.imageUrl && <img src={post.imageUrl} alt="Post" />}
              </div>
              <div className="post-actions">
                <button onClick={() => likeMutation.mutate(post.id)}>
                  ❤️ {post.likes || 0}
                </button>
                <button>💬 {post.comments || 0}</button>
                <button>🔄 Share</button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default SocialFeed;

