/**
 * OASIS Avatar SSO Web Component
 * Framework-agnostic authentication component
 */
class OasisAvatarSSO extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this._providers = [];
    this._loading = null;
    this._isOpen = false;
  }

  static get observedAttributes() {
    return ['providers', 'theme'];
  }

  connectedCallback() {
    const providersAttr = this.getAttribute('providers') || 'holochain,ethereum,solana';
    this._providers = providersAttr.split(',').map(p => p.trim());
    this.render();
    this.attachEventListeners();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue) {
      if (name === 'providers') {
        this._providers = newValue.split(',').map(p => p.trim());
      }
      this.render();
    }
  }

  async handleProviderLogin(provider) {
    this._loading = provider;
    this.render();

    try {
      const response = await fetch('https://api.oasis.network/auth/provider', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider })
      });
      
      const result = await response.json();
      
      this.dispatchEvent(new CustomEvent('success', {
        detail: result,
        bubbles: true
      }));
      
      this._isOpen = false;
    } catch (error) {
      this.dispatchEvent(new CustomEvent('error', {
        detail: error,
        bubbles: true
      }));
    } finally {
      this._loading = null;
      this.render();
    }
  }

  attachEventListeners() {
    const triggerBtn = this.shadowRoot.querySelector('.trigger-btn');
    if (triggerBtn) {
      triggerBtn.addEventListener('click', () => {
        this._isOpen = true;
        this.render();
      });
    }

    const closeBtn = this.shadowRoot.querySelector('.close-btn');
    if (closeBtn) {
      closeBtn.addEventListener('click', () => {
        this._isOpen = false;
        this.render();
      });
    }

    const providerBtns = this.shadowRoot.querySelectorAll('.provider-btn');
    providerBtns.forEach((btn, index) => {
      btn.addEventListener('click', () => {
        this.handleProviderLogin(this._providers[index]);
      });
    });
  }

  render() {
    const theme = this.getAttribute('theme') || 'dark';
    
    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: inline-block;
        }
        .trigger-btn {
          padding: 0.75rem 1.5rem;
          background: #00bcd4;
          color: white;
          border: none;
          border-radius: 8px;
          font-size: 1rem;
          cursor: pointer;
          transition: background 0.2s;
        }
        .trigger-btn:hover {
          background: #0097a7;
        }
        .modal {
          position: fixed;
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          background: ${theme === 'dark' ? '#1a1a1a' : 'white'};
          color: ${theme === 'dark' ? 'white' : '#333'};
          padding: 2rem;
          border-radius: 12px;
          box-shadow: 0 10px 40px rgba(0,0,0,0.3);
          z-index: 1000;
          min-width: 300px;
        }
        .modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 1.5rem;
        }
        .close-btn {
          background: none;
          border: none;
          color: ${theme === 'dark' ? 'white' : '#333'};
          font-size: 1.5rem;
          cursor: pointer;
        }
        .providers {
          display: flex;
          flex-direction: column;
          gap: 0.75rem;
        }
        .provider-btn {
          padding: 0.75rem 1rem;
          background: ${theme === 'dark' ? '#2a2a2a' : '#f5f5f5'};
          border: 1px solid ${theme === 'dark' ? '#3a3a3a' : '#ddd'};
          border-radius: 8px;
          color: ${theme === 'dark' ? 'white' : '#333'};
          cursor: pointer;
          transition: all 0.2s;
          text-transform: capitalize;
        }
        .provider-btn:hover:not(:disabled) {
          background: #00bcd4;
          border-color: #00bcd4;
          color: white;
        }
        .provider-btn:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }
      </style>

      <button class="trigger-btn">Sign In with OASIS</button>

      ${this._isOpen ? `
        <div class="modal">
          <div class="modal-header">
            <h2>Sign In to OASIS</h2>
            <button class="close-btn">×</button>
          </div>
          <div class="providers">
            ${this._providers.map(provider => `
              <button class="provider-btn" ${this._loading === provider ? 'disabled' : ''}>
                ${this._loading === provider ? 'Connecting...' : `Connect with ${provider}`}
              </button>
            `).join('')}
          </div>
        </div>
      ` : ''}
    `;

    // Re-attach event listeners after render
    if (this._isOpen) {
      setTimeout(() => this.attachEventListeners(), 0);
    } else {
      const triggerBtn = this.shadowRoot.querySelector('.trigger-btn');
      if (triggerBtn) {
        triggerBtn.addEventListener('click', () => {
          this._isOpen = true;
          this.render();
        });
      }
    }
  }
}

customElements.define('oasis-avatar-sso', OasisAvatarSSO);

