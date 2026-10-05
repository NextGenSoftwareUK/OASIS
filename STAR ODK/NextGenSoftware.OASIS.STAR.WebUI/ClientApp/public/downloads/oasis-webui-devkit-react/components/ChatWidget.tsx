import React, { useState } from 'react';
import { Messaging } from './Messaging';

export interface ChatWidgetProps {
  chatId: string;
  avatarId: string;
  position?: 'bottom-right' | 'bottom-left';
  defaultOpen?: boolean;
  enableNotifications?: boolean;
  theme?: 'light' | 'dark';
  customStyles?: React.CSSProperties;
}

export const ChatWidget: React.FC<ChatWidgetProps> = ({
  chatId,
  avatarId,
  position = 'bottom-right',
  defaultOpen = false,
  enableNotifications = true,
  theme = 'dark',
  customStyles = {}
}) => {
  const [isOpen, setIsOpen] = useState(defaultOpen);
  const [unreadCount, setUnreadCount] = useState(0);

  return (
    <div 
      className={`oasis-chat-widget oasis-chat-widget--${position}`}
      style={customStyles}
    >
      {!isOpen && (
        <button 
          className="chat-widget__toggle"
          onClick={() => setIsOpen(true)}
        >
          💬
          {unreadCount > 0 && (
            <span className="badge">{unreadCount}</span>
          )}
        </button>
      )}

      {isOpen && (
        <div className="chat-widget__container">
          <div className="chat-widget__header">
            <span>Chat</span>
            <button onClick={() => setIsOpen(false)}>×</button>
          </div>
          <Messaging
            chatId={chatId}
            avatarId={avatarId}
            position="inline"
            theme={theme}
          />
        </div>
      )}
    </div>
  );
};

export default ChatWidget;

