class OASISKarmaManagement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.karma = { total: 0, positive: 0, negative: 0, level: 1 };
  }

  connectedCallback() {
    this.render();
    this.loadKarma();
  }

  async loadKarma() {
    const response = await fetch('/api/karma');
    this.karma = await response.json();
    this.render();
  }

  render() {
    const levelProgress = ((this.karma.total % 1000) / 1000) * 100;
    
    this.shadowRoot.innerHTML = `
      <style>
        .karma-header { background: linear-gradient(135deg, #667eea, #764ba2); color: white; padding: 32px; border-radius: 12px; margin-bottom: 24px; }
        .karma-total { font-size: 48px; margin: 0; }
        .level-badge { background: rgba(255,255,255,0.2); padding: 4px 12px; border-radius: 16px; }
        .progress-bar { height: 8px; background: rgba(255,255,255,0.2); border-radius: 4px; margin: 12px 0; overflow: hidden; }
        .progress { height: 100%; background: white; }
        .breakdown { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
        .type { padding: 20px; border-radius: 8px; }
        .type.positive { background: #e8f5e9; }
        .type.negative { background: #ffebee; }
      </style>
      <div class="karma-header">
        <h2 class="karma-total">${this.karma.total.toLocaleString()}</h2>
        <p>Total Karma</p>
        <span class="level-badge">Level ${this.karma.level}</span>
        <div class="progress-bar"><div class="progress" style="width: ${levelProgress}%"></div></div>
      </div>
      <div class="breakdown">
        <div class="type positive"><strong>Positive</strong><br/>${this.karma.positive}</div>
        <div class="type negative"><strong>Negative</strong><br/>${this.karma.negative}</div>
      </div>
    `;
  }
}

customElements.define('oasis-karma-management', OASISKarmaManagement);



