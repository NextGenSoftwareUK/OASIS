class OASISKarmaLeaderboard extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.leaderboard = [];
  }

  connectedCallback() {
    this.render();
    this.loadLeaderboard();
  }

  async loadLeaderboard() {
    const response = await fetch('/api/karma/leaderboard');
    this.leaderboard = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; background: white; border-radius: 12px; padding: 20px; }
        .leader { display: flex; align-items: center; gap: 16px; padding: 12px; border-bottom: 1px solid #eee; }
        .rank { font-size: 24px; font-weight: 600; width: 40px; }
        .rank.gold { color: #f39c12; }
        .rank.silver { color: #95a5a6; }
        .rank.bronze { color: #cd7f32; }
        .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; display: flex; align-items: center; justify-content: center; font-weight: 600; }
        .info { flex: 1; }
        .karma { font-size: 18px; font-weight: 600; color: #667eea; }
      </style>
      <h2>Karma Leaderboard</h2>
      ${this.leaderboard.map((l, i) => `
        <div class="leader">
          <div class="rank ${i === 0 ? 'gold' : i === 1 ? 'silver' : i === 2 ? 'bronze' : ''}">${i + 1}</div>
          <div class="avatar">${l.username.charAt(0)}</div>
          <div class="info">
            <div>${l.username}</div>
            <small>Level ${l.level}</small>
          </div>
          <div class="karma">${l.karma.toLocaleString()}</div>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-karma-leaderboard', OASISKarmaLeaderboard);



