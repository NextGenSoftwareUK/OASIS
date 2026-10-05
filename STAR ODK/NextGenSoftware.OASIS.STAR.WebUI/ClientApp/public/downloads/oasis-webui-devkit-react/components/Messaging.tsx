import React, { useState, useEffect, useRef } from 'react';
import { OASISClient } from '@oasis/api-client';

export interface MessagingProps {
  chatId: string;
  avatarId: string;
  position?: 'bottom-right' | 'bottom-left' | 'inline';
  theme?: 'light' | 'dark';
  enableEmojis?: boolean;
  enableFileSharing?: boolean;
  customStyles?: React.CSSProperties;
}

export const Messaging: React.FC<MessagingProps> = ({
  chatId,
  avatarId,
  position = 'bottom-right',
  theme = 'dark',
  enableEmojis = true,
  enableFileSharing = true,
  customStyles = {}
}) => {
  const [messages, setMessages] = useState<any[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const client = new OASISClient();

  useEffect(() => {
    // Load initial messages
    loadMessages();
    
    // Subscribe to real-time updates
    const unsubscribe = client.subscribeToChat(chatId, (message: any) => {
      setMessages(prev => [...prev, message]);
    });

    return () => unsubscribe();
  }, [chatId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const loadMessages = async () => {
    const result = await client.getChatMessages(chatId);
    setMessages(result);
  };

  const sendMessage = async () => {
    if (!newMessage.trim()) return;

    await client.sendMessage({
      chatId,
      avatarId,
      content: newMessage,
      timestamp: new Date().toISOString()
    });

    setNewMessage('');
  };

  return (
    <div 
      className={`oasis-messaging oasis-messaging--${theme} oasis-messaging--${position}`}
      style={customStyles}
    >
      {position !== 'inline' && (
        <button 
          className="oasis-messaging__toggle"
          onClick={() => setIsOpen(!isOpen)}
        >
          💬
        </button>
      )}

      {(isOpen || position === 'inline') && (
        <div className="oasis-messaging__container">
          <div className="oasis-messaging__header">
            <h3>Chat</h3>
            {position !== 'inline' && (
              <button onClick={() => setIsOpen(false)}>×</button>
            )}
          </div>

          <div className="oasis-messaging__messages">
            {messages.map((msg, idx) => (
              <div 
                key={idx}
                className={`message ${msg.avatarId === avatarId ? 'message--own' : 'message--other'}`}
              >
                <div className="message-avatar">
                  <img src={msg.avatarImage} alt={msg.avatarName} />
                </div>
                <div className="message-content">
                  <div className="message-author">{msg.avatarName}</div>
                  <div className="message-text">{msg.content}</div>
                  <div className="message-time">
                    {new Date(msg.timestamp).toLocaleTimeString()}
                  </div>
                </div>
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>

          <div className="oasis-messaging__input">
            <input
              type="text"
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
              placeholder="Type a message..."
            />
            {enableEmojis && (
              <button className="emoji-btn">😊</button>
            )}
            {enableFileSharing && (
              <button className="file-btn">📎</button>
            )}
            <button onClick={sendMessage}>Send</button>
          </div>
        </div>
      )}
    </div>
  );
};

export default Messaging;

