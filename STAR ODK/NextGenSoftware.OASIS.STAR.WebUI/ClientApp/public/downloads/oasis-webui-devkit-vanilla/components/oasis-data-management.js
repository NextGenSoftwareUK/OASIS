class OASISDataManagement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.holons = [];
  }

  connectedCallback() {
    this.render();
    this.loadHolons();
  }

  async loadHolons() {
    const response = await fetch('/api/data/holons');
    this.holons = await response.json();
    this.render();
  }

  async createHolon(name, type) {
    await fetch('/api/data/holons', { method: 'POST', body: JSON.stringify({ name, holonType: type }) });
    this.loadHolons();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; padding: 20px; }
        .holon-card { background: white; padding: 16px; margin-bottom: 12px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .create-form { display: flex; gap: 8px; margin-bottom: 20px; }
        input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        button { padding: 10px 20px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
      </style>
      <div class="create-form">
        <input id="nameInput" placeholder="Holon Name" />
        <input id="typeInput" placeholder="Type" />
        <button id="createBtn">Create</button>
      </div>
      <div class="holons">
        ${this.holons.map(h => `
          <div class="holon-card">
            <h3>${h.name}</h3>
            <p>Type: ${h.holonType}</p>
            <small>ID: ${h.id}</small>
          </div>
        `).join('')}
      </div>
    `;
    this.shadowRoot.getElementById('createBtn')?.addEventListener('click', () => {
      const name = this.shadowRoot.getElementById('nameInput').value;
      const type = this.shadowRoot.getElementById('typeInput').value;
      this.createHolon(name, type);
    });
  }
}

customElements.define('oasis-data-management', OASISDataManagement);



