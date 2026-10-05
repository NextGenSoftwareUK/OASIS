class OASISGeoNFTMap extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
  }

  connectedCallback() {
    this.render();
    this.initMap();
  }

  initMap() {
    // Map initialization
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; height: 500px; position: relative; background: #e0e0e0; border-radius: 12px; overflow: hidden; }
        .controls { position: absolute; top: 20px; right: 20px; display: flex; gap: 8px; z-index: 10; }
        button { padding: 10px 16px; background: white; border: none; border-radius: 4px; cursor: pointer; box-shadow: 0 2px 4px rgba(0,0,0,0.2); }
      </style>
      <div id="map" style="width: 100%; height: 100%;"></div>
      <div class="controls">
        <button>📍 My Location</button>
        <button>🔥 Heatmap</button>
      </div>
    `;
  }
}

customElements.define('oasis-geonft-map', OASISGeoNFTMap);



