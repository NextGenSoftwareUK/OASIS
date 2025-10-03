class OASISFriendsList extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.friends = [];
  }

  connectedCallback() {
    this.render();
    this.loadFriends();
  }

  async loadFriends() {
    const response = await fetch('/api/friends');
    this.friends = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .friend { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; align-items: center; }
        .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; }
        .status { width: 12px; height: 12px; border-radius: 50%; }
        .status.online { background: #27ae60; }
        .status.offline { background: #95a5a6; }
      </style>
      ${this.friends.map(f => `
        <div class="friend">
          <div class="avatar"></div>
          <div style="flex: 1;"><strong>${f.username}</strong><br/><small>Level ${f.level}</small></div>
          <div class="status ${f.online ? 'online' : 'offline'}"></div>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-friends-list', OASISFriendsList);



