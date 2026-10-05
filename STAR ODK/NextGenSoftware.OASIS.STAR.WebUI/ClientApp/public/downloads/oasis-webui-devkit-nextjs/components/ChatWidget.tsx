import { useState } from 'react';

export default function ChatWidget() {
  const [minimized, setMinimized] = useState(false);
  const [newMessage, setNewMessage] = useState('');
  const [messages, setMessages] = useState([]);

  function send() {
    if (!newMessage.trim()) return;
    setMessages([...messages, { sender: 'You', text: newMessage }]);
    setNewMessage('');
  }

  return (
    <div className={`chat-widget ${minimized ? 'minimized' : ''}`}>
      <div className="header" onClick={() => setMinimized(!minimized)}>
        <span>💬 Chat</span>
        <span>{minimized ? '▲' : '▼'}</span>
      </div>
      {!minimized && (
        <div className="body">
          <div className="messages">
            {messages.map((msg: any, i) => (
              <div key={i}><strong>{msg.sender}:</strong> {msg.text}</div>
            ))}
          </div>
          <div className="input-area">
            <input value={newMessage} onChange={(e) => setNewMessage(e.target.value)} onKeyPress={(e) => e.key === 'Enter' && send()} placeholder="Type..." />
            <button onClick={send}>Send</button>
          </div>
        </div>
      )}
      <style jsx>{`
        .chat-widget { position: fixed; bottom: 20px; right: 20px; width: 350px; background: white; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,0.2); z-index: 1000; }
        .chat-widget.minimized { width: 200px; }
        .header { background: #4A90E2; color: white; padding: 16px; border-radius: 12px 12px 0 0; display: flex; justify-content: space-between; cursor: pointer; }
        .body { height: 400px; display: flex; flex-direction: column; }
        .messages { flex: 1; overflow-y: auto; padding: 16px; }
        .input-area { display: flex; gap: 8px; padding: 16px; border-top: 1px solid #eee; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 6px; }
        button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; }
      `}</style>
    </div>
  );
}



