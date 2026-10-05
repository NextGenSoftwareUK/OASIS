class OASISMessaging extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.messages = [];
  }

  connectedCallback() {
    this.render();
    this.loadMessages();
  }

  async loadMessages() {
    const response = await fetch('/api/messages');
    this.messages = await response.json();
    this.render();
  }

  async sendMessage(text) {
    await fetch('/api/messages', { method: 'POST', body: JSON.stringify({ text }) });
    this.loadMessages();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; padding: 20px; }
        .message { padding: 12px; border-bottom: 1px solid #eee; }
        .input-area { display: flex; gap: 8px; margin-top: 16px; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 4px; cursor: pointer; }
      </style>
      <div class="messages">
        ${this.messages.map(m => `<div class="message"><strong>${m.sender}:</strong> ${m.text}</div>`).join('')}
      </div>
      <div class="input-area">
        <input id="msgInput" placeholder="Type a message..." />
        <button id="sendBtn">Send</button>
      </div>
    `;
    this.shadowRoot.getElementById('sendBtn')?.addEventListener('click', () => {
      const input = this.shadowRoot.getElementById('msgInput');
      this.sendMessage(input.value);
      input.value = '';
    });
  }
}

customElements.define('oasis-messaging', OASISMessaging);



