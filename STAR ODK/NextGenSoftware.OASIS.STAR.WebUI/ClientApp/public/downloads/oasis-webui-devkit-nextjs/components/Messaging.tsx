import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function Messaging() {
  const { client } = useOASIS();
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');

  useEffect(() => {
    loadMessages();
  }, []);

  async function loadMessages() {
    const response = await client.get('/api/messages');
    setMessages(response.data);
  }

  async function sendMessage() {
    if (!newMessage.trim()) return;
    await client.post('/api/messages', { text: newMessage });
    setNewMessage('');
    loadMessages();
  }

  return (
    <div className="oasis-messaging">
      <div className="messages">
        {messages.map((msg: any, i) => (
          <div key={i} className="message">
            <strong>{msg.sender}:</strong> {msg.text}
          </div>
        ))}
      </div>
      <div className="input-area">
        <input 
          value={newMessage} 
          onChange={(e) => setNewMessage(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
          placeholder="Type a message..." 
        />
        <button onClick={sendMessage}>Send</button>
      </div>
      <style jsx>{`
        .oasis-messaging { padding: 20px; }
        .messages { max-height: 400px; overflow-y: auto; margin-bottom: 16px; }
        .message { padding: 12px; border-bottom: 1px solid #eee; }
        .input-area { display: flex; gap: 8px; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 4px; cursor: pointer; }
      `}</style>
    </div>
  );
}



