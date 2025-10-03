/**
 * OASIS Karma Display Web Component
 */
class OasisKarmaDisplay extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this._karma = null;
    this._loading = false;
  }

  static get observedAttributes() {
    return ['avatar-id', 'theme', 'show-history'];
  }

  connectedCallback() {
    this.render();
    this.loadKarma();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue && name === 'avatar-id') {
      this.loadKarma();
    }
  }

  async loadKarma() {
    const avatarId = this.getAttribute('avatar-id');
    if (!avatarId) return;

    this._loading = true;
    this.render();

    try {
      const response = await fetch(`https://api.oasis.network/avatars/${avatarId}/karma`);
      this._karma = await response.json();
    } catch (error) {
      console.error('Error loading karma:', error);
    } finally {
      this._loading = false;
      this.render();
    }
  }

  render() {
    const theme = this.getAttribute('theme') || 'dark';
    const showHistory = this.getAttribute('show-history') === 'true';

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          padding: 1.5rem;
          background: ${theme === 'dark' ? '#1a1a1a' : 'white'};
          color: ${theme === 'dark' ? 'white' : '#333'};
          border-radius: 12px;
        }
        .karma-current {
          text-align: center;
          margin-bottom: 1.5rem;
        }
        .karma-value {
          font-size: 3rem;
          font-weight: bold;
          color: #00bcd4;
          margin: 0.5rem 0;
        }
        .karma-stats {
          display: flex;
          justify-content: space-around;
          gap: 1rem;
        }
        .stat {
          text-align: center;
        }
        .stat .label {
          display: block;
          font-size: 0.875rem;
          opacity: 0.7;
          margin-bottom: 0.25rem;
        }
        .stat .value {
          font-size: 1.25rem;
          font-weight: bold;
        }
        .history {
          margin-top: 1.5rem;
          border-top: 1px solid ${theme === 'dark' ? '#2a2a2a' : '#ddd'};
          padding-top: 1rem;
        }
        .history-item {
          display: flex;
          justify-content: space-between;
          padding: 0.5rem 0;
          border-bottom: 1px solid ${theme === 'dark' ? '#2a2a2a' : '#f5f5f5'};
        }
      </style>

      <div class="karma-current">
        <h3>Karma Points</h3>
        ${this._loading ? 
          '<div class="karma-value">Loading...</div>' : 
          `<div class="karma-value">${this._karma?.total || 0}</div>`
        }
      </div>

      ${this._karma ? `
        <div class="karma-stats">
          <div class="stat">
            <span class="label">Rank</span>
            <span class="value">#${this._karma.rank || '-'}</span>
          </div>
          <div class="stat">
            <span class="label">Level</span>
            <span class="value">${this._karma.level || 1}</span>
          </div>
          <div class="stat">
            <span class="label">Next Level</span>
            <span class="value">${this._karma.nextLevelAt || '-'}</span>
          </div>
        </div>
      ` : ''}

      ${showHistory && this._karma?.history ? `
        <div class="history">
          <h4>Recent Activity</h4>
          ${this._karma.history.map(entry => `
            <div class="history-item">
              <span>${entry.reason}</span>
              <span style="color: ${entry.amount > 0 ? '#4caf50' : '#f44336'}">
                ${entry.amount > 0 ? '+' : ''}${entry.amount}
              </span>
            </div>
          `).join('')}
        </div>
      ` : ''}
    `;
  }
}

customElements.define('oasis-karma-display', OasisKarmaDisplay);

