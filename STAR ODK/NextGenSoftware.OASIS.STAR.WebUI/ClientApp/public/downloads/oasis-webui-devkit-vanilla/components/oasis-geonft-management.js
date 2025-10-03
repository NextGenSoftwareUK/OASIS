class OASISGeoNFTManagement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.geonfts = [];
  }

  connectedCallback() {
    this.render();
    this.loadGeoNFTs();
  }

  async loadGeoNFTs() {
    const response = await fetch('/api/geonft/nearby');
    this.geonfts = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: flex; height: 600px; }
        .map { flex: 1; background: #e0e0e0; }
        .sidebar { width: 300px; padding: 20px; background: white; overflow-y: auto; }
        .geonft { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
        .geonft:hover { background: #f5f5f5; }
        button { width: 100%; padding: 12px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; margin-bottom: 20px; }
      </style>
      <div class="map"></div>
      <div class="sidebar">
        <h3>GeoNFTs Nearby</h3>
        <button>Create GeoNFT</button>
        ${this.geonfts.map(g => `
          <div class="geonft">
            <div>📍</div>
            <div><strong>${g.name}</strong><br/><small>${g.distance}m away</small></div>
          </div>
        `).join('')}
      </div>
    `;
  }
}

customElements.define('oasis-geonft-management', OASISGeoNFTManagement);



