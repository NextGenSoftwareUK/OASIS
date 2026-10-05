class OASISAvatarCard extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const username = this.getAttribute('username') || 'User';
    const karma = this.getAttribute('karma') || '0';
    const level = this.getAttribute('level') || '1';

    this.shadowRoot.innerHTML = `
      <style>
        .card { background: white; padding: 20px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); text-align: center; }
        .avatar { width: 80px; height: 80px; border-radius: 50%; background: linear-gradient(135deg, #667eea, #764ba2); margin: 0 auto 16px; }
        .username { font-size: 18px; font-weight: 600; margin: 0 0 8px 0; }
        .stats { display: flex; justify-content: space-around; margin-top: 16px; }
        .stat { text-align: center; }
        .stat-value { font-size: 20px; font-weight: 600; color: #667eea; }
      </style>
      <div class="card">
        <div class="avatar"></div>
        <h3 class="username">${username}</h3>
        <div class="stats">
          <div class="stat"><div class="stat-value">${karma}</div><div>Karma</div></div>
          <div class="stat"><div class="stat-value">${level}</div><div>Level</div></div>
        </div>
      </div>
    `;
  }
}

customElements.define('oasis-avatar-card', OASISAvatarCard);



