class OASISNFTManagement extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.nfts = [];
  }

  connectedCallback() {
    this.render();
    this.loadNFTs();
  }

  async loadNFTs() {
    const response = await fetch('/api/nfts');
    this.nfts = await response.json();
    this.render();
  }

  render() {
    this.shadowRoot.innerHTML = `
      <style>
        .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 20px; }
        .nft-card { background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        .nft-image { aspect-ratio: 1; background: #f5f5f5; }
        .nft-info { padding: 16px; }
        .chain-badge { background: #e3f2fd; color: #1976d2; padding: 4px 8px; border-radius: 4px; font-size: 12px; }
      </style>
      <h2>My NFTs</h2>
      <div class="grid">
        ${this.nfts.map(nft => `
          <div class="nft-card">
            <div class="nft-image"></div>
            <div class="nft-info">
              <h3>${nft.name}</h3>
              <span class="chain-badge">${nft.chain}</span>
            </div>
          </div>
        `).join('')}
      </div>
    `;
  }
}

customElements.define('oasis-nft-management', OASISNFTManagement);



