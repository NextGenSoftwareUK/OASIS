import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function Notifications() {
  const { client } = useOASIS();
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    loadNotifications();
  }, []);

  async function loadNotifications() {
    const response = await client.get('/api/notifications');
    setNotifications(response.data);
  }

  async function markAsRead(id: string) {
    await client.post(`/api/notifications/${id}/read`);
    loadNotifications();
  }

  return (
    <div className="oasis-notifications">
      <h3>Notifications</h3>
      {notifications.map((notif: any) => (
        <div key={notif.id} className={`notification ${notif.read ? '' : 'unread'}`} onClick={() => markAsRead(notif.id)}>
          <div className="icon">{notif.type === 'karma' ? '⭐' : '📢'}</div>
          <div className="content">
            <h4>{notif.title}</h4>
            <p>{notif.message}</p>
          </div>
        </div>
      ))}
      <style jsx>{`
        .notification { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
        .notification:hover { background: #f5f5f5; }
        .notification.unread { background: #e3f2fd; }
        .icon { font-size: 24px; }
        .content h4 { margin: 0 0 4px 0; font-size: 14px; }
        .content p { margin: 0; font-size: 13px; color: #666; }
      `}</style>
    </div>
  );
}



