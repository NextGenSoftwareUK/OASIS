class OASISAchievements extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.achievements = [];
  }

  connectedCallback() {
    this.render();
    this.loadAchievements();
  }

  async loadAchievements() {
    const response = await fetch('/api/achievements');
    this.achievements = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .achievement { display: flex; gap: 12px; padding: 16px; background: white; margin-bottom: 12px; border-radius: 8px; }
        .achievement.locked { opacity: 0.5; }
        .icon { font-size: 48px; }
        .badge { position: absolute; background: #27ae60; color: white; border-radius: 50%; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; }
      </style>
      ${this.achievements.map(a => `
        <div class="achievement ${a.unlocked ? '' : 'locked'}">
          <div class="icon">${a.icon}${a.unlocked ? '<span class="badge">✓</span>' : ''}</div>
          <div><h3>${a.name}</h3><p>${a.description}</p></div>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-achievements', OASISAchievements);



