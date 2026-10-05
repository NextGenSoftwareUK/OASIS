class OASISProviderManagement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.providers = [];
  }

  connectedCallback() {
    this.render();
    this.loadProviders();
  }

  async loadProviders() {
    const response = await fetch('/api/providers');
    this.providers = await response.json();
    this.render();
  }

  async toggleProvider(id) {
    await fetch(`/api/providers/${id}/toggle`, { method: 'POST' });
    this.loadProviders();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .provider-card { display: flex; justify-content: space-between; align-items: center; padding: 16px; background: white; margin-bottom: 12px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .status { padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; }
        .status.active { background: #d4edda; color: #155724; }
        .status.inactive { background: #f8d7da; color: #721c24; }
        button { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; background: #4A90E2; color: white; }
      </style>
      ${this.providers.map(p => `
        <div class="provider-card">
          <div>
            <h3>${p.name}</h3>
            <span class="status ${p.isActive ? 'active' : 'inactive'}">${p.isActive ? 'Active' : 'Inactive'}</span>
          </div>
          <button onclick="this.getRootNode().host.toggleProvider('${p.id}')">Toggle</button>
        </div>
      `).join('')}
    `;
  }
}

customElements.define('oasis-provider-management', OASISProviderManagement);



