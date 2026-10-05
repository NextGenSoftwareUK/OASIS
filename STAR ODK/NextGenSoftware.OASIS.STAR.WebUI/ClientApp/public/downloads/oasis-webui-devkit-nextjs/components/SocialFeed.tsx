import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function SocialFeed() {
  const { client } = useOASIS();
  const [posts, setPosts] = useState([]);

  useEffect(() => {
    loadFeed();
  }, []);

  async function loadFeed() {
    const response = await client.get('/api/social/feed');
    setPosts(response.data);
  }

  return (
    <div className="social-feed">
      {posts.map((post: any) => (
        <div key={post.id} className="post">
          <div className="post-header">
            <div className="avatar"></div>
            <div><strong>{post.author}</strong><br/><small>{post.time}</small></div>
          </div>
          <p>{post.content}</p>
          <div className="actions">
            <button>👍 {post.likes}</button>
            <button>💬 {post.comments}</button>
          </div>
        </div>
      ))}
      <style jsx>{`
        .post { background: white; padding: 20px; margin-bottom: 16px; border-radius: 8px; }
        .post-header { display: flex; gap: 12px; margin-bottom: 12px; }
        .avatar { width: 40px; height: 40px; border-radius: 50%; background: #e3f2fd; }
        .actions { display: flex; gap: 16px; margin-top: 12px; }
        .actions button { background: none; border: none; cursor: pointer; }
      `}</style>
    </div>
  );
}



