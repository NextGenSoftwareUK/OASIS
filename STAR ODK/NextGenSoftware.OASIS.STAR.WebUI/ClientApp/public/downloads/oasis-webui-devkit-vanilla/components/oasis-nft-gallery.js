/**
 * OASIS NFT Gallery Web Component
 */
class OasisNFTGallery extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this._nfts = [];
    this._loading = false;
  }

  static get observedAttributes() {
    return ['avatar-id', 'columns', 'theme'];
  }

  connectedCallback() {
    this.render();
    this.loadNFTs();
  }

  async loadNFTs() {
    const avatarId = this.getAttribute('avatar-id');
    if (!avatarId) return;

    this._loading = true;
    this.render();

    try {
      const response = await fetch(`https://api.oasis.network/nfts?avatarId=${avatarId}`);
      this._nfts = await response.json();
    } catch (error) {
      console.error('Error loading NFTs:', error);
    } finally {
      this._loading = false;
      this.render();
    }
  }

  handleNFTClick(nft) {
    this.dispatchEvent(new CustomEvent('select', {
      detail: nft,
      bubbles: true
    }));
  }

  render() {
    const theme = this.getAttribute('theme') || 'dark';
    const columns = parseInt(this.getAttribute('columns') || '3');

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          padding: 1rem;
        }
        .nft-grid {
          display: grid;
          grid-template-columns: repeat(${columns}, 1fr);
          gap: 1rem;
        }
        .nft-item {
          background: ${theme === 'dark' ? '#2a2a2a' : 'white'};
          border: 1px solid ${theme === 'dark' ? '#3a3a3a' : '#ddd'};
          border-radius: 8px;
          overflow: hidden;
          cursor: pointer;
          transition: transform 0.2s;
        }
        .nft-item:hover {
          transform: translateY(-4px);
        }
        .nft-image {
          width: 100%;
          height: 200px;
          object-fit: cover;
        }
        .nft-info {
          padding: 1rem;
          color: ${theme === 'dark' ? 'white' : '#333'};
        }
        .nft-name {
          font-weight: bold;
          margin-bottom: 0.5rem;
        }
        .nft-price {
          color: #00bcd4;
        }
        .loading {
          text-align: center;
          padding: 2rem;
          color: ${theme === 'dark' ? 'white' : '#333'};
        }
      </style>

      ${this._loading ? 
        '<div class="loading">Loading NFTs...</div>' :
        `<div class="nft-grid">
          ${this._nfts.map((nft, index) => `
            <div class="nft-item" data-index="${index}">
              <img class="nft-image" src="${nft.imageUrl}" alt="${nft.name}" />
              <div class="nft-info">
                <div class="nft-name">${nft.name}</div>
                <div class="nft-collection">${nft.collection || ''}</div>
                <div class="nft-price">${nft.price || '0'} OASIS</div>
              </div>
            </div>
          `).join('')}
        </div>`
      }
    `;

    // Attach click listeners
    const nftItems = this.shadowRoot.querySelectorAll('.nft-item');
    nftItems.forEach((item, index) => {
      item.addEventListener('click', () => {
        this.handleNFTClick(this._nfts[index]);
      });
    });
  }
}

customElements.define('oasis-nft-gallery', OasisNFTGallery);

