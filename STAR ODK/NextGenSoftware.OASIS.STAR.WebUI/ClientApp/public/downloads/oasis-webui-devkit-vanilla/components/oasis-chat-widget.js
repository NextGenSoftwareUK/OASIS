class OASISChatWidget extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.minimized = false;
  }

  connectedCallback() {
    this.render();
  }

  toggle() {
    this.minimized = !this.minimized;
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { position: fixed; bottom: 20px; right: 20px; width: ${this.minimized ? '200px' : '350px'}; background: white; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,0.2); z-index: 1000; }
        .header { background: #4A90E2; color: white; padding: 16px; border-radius: 12px 12px 0 0; display: flex; justify-content: space-between; cursor: pointer; }
        .body { display: ${this.minimized ? 'none' : 'flex'}; flex-direction: column; height: 400px; }
        .messages { flex: 1; overflow-y: auto; padding: 16px; }
        .input-area { display: flex; gap: 8px; padding: 16px; border-top: 1px solid #eee; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 6px; }
        button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; }
      </style>
      <div class="header" onclick="this.getRootNode().host.toggle()">
        <span>💬 Chat</span>
        <span>${this.minimized ? '▲' : '▼'}</span>
      </div>
      <div class="body">
        <div class="messages">
          <div>No messages yet...</div>
        </div>
        <div class="input-area">
          <input placeholder="Type a message..." />
          <button>Send</button>
        </div>
      </div>
    `;
  }
}

customElements.define('oasis-chat-widget', OASISChatWidget);



