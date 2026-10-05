class OASISAvatarDetail extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.avatar = null;
  }

  connectedCallback() {
    this.render();
    this.loadAvatar();
  }

  async loadAvatar() {
    const avatarId = this.getAttribute('avatar-id');
    const response = await fetch(`/api/avatar/${avatarId}`);
    this.avatar = await response.json();
    this.render();
  }

  render() {
    if (!this.avatar) {
      this.shadowRoot.innerHTML = '<div>Loading...</div>';
      return;
    }
    
    this.shadowRoot.innerHTML = `
      <style>
        .profile { background: white; padding: 24px; border-radius: 12px; }
        .header { display: flex; gap: 20px; margin-bottom: 20px; }
        .avatar { width: 100px; height: 100px; border-radius: 50%; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
        .stat { text-align: center; padding: 16px; background: #f5f5f5; border-radius: 8px; }
        .stat-value { font-size: 24px; font-weight: 600; color: #667eea; }
      </style>
      <div class="profile">
        <div class="header">
          <div class="avatar"></div>
          <div>
            <h2>${this.avatar.username}</h2>
            <p>${this.avatar.email}</p>
            <p>Level ${this.avatar.level}</p>
          </div>
        </div>
        <div class="stats">
          <div class="stat"><div class="stat-value">${this.avatar.karma}</div><div>Karma</div></div>
          <div class="stat"><div class="stat-value">${this.avatar.nftCount || 0}</div><div>NFTs</div></div>
          <div class="stat"><div class="stat-value">${this.avatar.achievements || 0}</div><div>Achievements</div></div>
        </div>
      </div>
    `;
  }
}

customElements.define('oasis-avatar-detail', OASISAvatarDetail);



