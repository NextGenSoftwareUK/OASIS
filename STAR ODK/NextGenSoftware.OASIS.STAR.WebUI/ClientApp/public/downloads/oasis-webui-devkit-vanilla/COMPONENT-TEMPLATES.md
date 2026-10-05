# Vanilla JS Web Components Templates

Create framework-agnostic OASIS components using Web Components.

## Base Web Component Template

```javascript
class OasisComponentName extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this._data = null;
    this._loading = false;
  }

  static get observedAttributes() {
    return ['avatar-id', 'theme', 'custom-attribute'];
  }

  connectedCallback() {
    this.render();
    this.loadData();
    this.attachEventListeners();
  }

  disconnectedCallback() {
    this.removeEventListeners();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue) {
      this.render();
    }
  }

  async loadData() {
    this._loading = true;
    this.render();
    
    try {
      const response = await fetch(`https://api.oasis.network/endpoint/${this.getAttribute('avatar-id')}`);
      this._data = await response.json();
    } catch (error) {
      this.dispatchEvent(new CustomEvent('error', { detail: error }));
    } finally {
      this._loading = false;
      this.render();
    }
  }

  attachEventListeners() {
    // Add your event listeners
  }

  removeEventListeners() {
    // Clean up event listeners
  }

  handleAction(event) {
    // Handle user interactions
    this.dispatchEvent(new CustomEvent('action', { 
      detail: { /* your data */ },
      bubbles: true 
    }));
  }

  render() {
    const theme = this.getAttribute('theme') || 'dark';
    
    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          padding: 1.5rem;
        }
        .container {
          font-family: system-ui, -apple-system, sans-serif;
        }
        .container.dark {
          background: #1a1a1a;
          color: white;
        }
        /* Add your styles */
      </style>
      
      <div class="container ${theme}">
        ${this._loading ? '<div>Loading...</div>' : this.renderContent()}
      </div>
    `;
  }

  renderContent() {
    if (!this._data) return '<div>No data</div>';
    
    return `
      <div class="content">
        <!-- Your component HTML -->
      </div>
    `;
  }
}

// Register the component
customElements.define('oasis-component-name', OasisComponentName);
```

## Priority Components

### 1. oasis-avatar-sso.js
```javascript
class OasisAvatarSSO extends HTMLElement {
  // Authentication with multiple providers
}
customElements.define('oasis-avatar-sso', OasisAvatarSSO);
```

### 2. oasis-karma-display.js
```javascript
class OasisKarmaDisplay extends HTMLElement {
  // Show karma with animations
}
customElements.define('oasis-karma-display', OasisKarmaDisplay);
```

### 3. oasis-nft-card.js
```javascript
class OasisNFTCard extends HTMLElement {
  // Display single NFT
}
customElements.define('oasis-nft-card', OasisNFTCard);
```

### 4. oasis-chat.js
```javascript
class OasisChat extends HTMLElement {
  // Real-time messaging
}
customElements.define('oasis-chat', OasisChat);
```

## Usage Examples

```html
<!-- In your HTML -->
<oasis-avatar-sso 
  providers="holochain,ethereum,solana"
  theme="dark">
</oasis-avatar-sso>

<oasis-karma-display 
  avatar-id="123"
  show-history="true">
</oasis-karma-display>

<!-- Listen to events -->
<script>
  document.querySelector('oasis-avatar-sso')
    .addEventListener('success', (e) => {
      console.log('User logged in:', e.detail);
    });
</script>
```

## Build Instructions

1. Copy base template
2. Rename class and element name
3. Define observedAttributes
4. Implement render logic
5. Add event handlers
6. Register with customElements.define()
7. Include in your page

## Remaining Components

- oasis-avatar-detail
- oasis-nft-gallery
- oasis-messaging
- oasis-data-manager
- oasis-provider-selector
- oasis-settings-panel
- oasis-notifications
- oasis-social-feed
- oasis-friends-list
- oasis-group-manager
- oasis-leaderboard
- oasis-achievements
- oasis-geonft-map
- oasis-geonft-creator

