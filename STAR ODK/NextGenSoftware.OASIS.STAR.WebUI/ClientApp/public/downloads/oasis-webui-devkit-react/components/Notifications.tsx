import React, { useEffect, useState } from 'react';
import { OASISClient } from '@oasis/api-client';

export interface NotificationsProps {
  avatarId: string;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
  autoHide?: boolean;
  duration?: number;
  theme?: 'light' | 'dark';
  customStyles?: React.CSSProperties;
}

export const Notifications: React.FC<NotificationsProps> = ({
  avatarId,
  position = 'top-right',
  autoHide = true,
  duration = 5000,
  theme = 'dark',
  customStyles = {}
}) => {
  const [notifications, setNotifications] = useState<any[]>([]);
  const client = new OASISClient();

  useEffect(() => {
    const unsubscribe = client.subscribeToNotifications(avatarId, (notification: any) => {
      setNotifications(prev => [notification, ...prev]);
      
      if (autoHide) {
        setTimeout(() => {
          setNotifications(prev => prev.filter(n => n.id !== notification.id));
        }, duration);
      }
    });

    return () => unsubscribe();
  }, [avatarId, autoHide, duration]);

  const handleDismiss = (id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  };

  return (
    <div 
      className={`oasis-notifications oasis-notifications--${position} oasis-notifications--${theme}`}
      style={customStyles}
    >
      {notifications.map(notification => (
        <div 
          key={notification.id}
          className={`notification notification--${notification.type || 'info'}`}
        >
          <div className="notification-content">
            <h4>{notification.title}</h4>
            <p>{notification.message}</p>
          </div>
          <button 
            className="dismiss-btn"
            onClick={() => handleDismiss(notification.id)}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  );
};

export default Notifications;

