# OASIS Web UI Dev Kit - Vanilla JavaScript

Framework-agnostic JavaScript library for OASIS integration - works with any web project!

## 🚀 Quick Start

```html
<!-- Via CDN -->
<script src="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.js"></script>
<link rel="stylesheet" href="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.css">

<!-- Or via npm -->
<script type="module">
  import OASIS from '@oasis/webui-devkit-vanilla';
</script>
```

## 📦 Components Included

All components work as Web Components (Custom Elements):

### Authentication
- `<oasis-avatar-sso>` - Multi-provider authentication
- `<oasis-avatar-detail>` - Profile management
- `<oasis-avatar-card>` - Avatar display

### Data & Storage
- `<oasis-data-management>` - Data operations
- `<oasis-provider-management>` - Provider switching
- `<oasis-settings>` - Settings panel

### Karma & Gamification
- `<oasis-karma-management>` - Karma display
- `<oasis-karma-leaderboard>` - Rankings
- `<oasis-achievements>` - Achievements

### NFT & Assets
- `<oasis-nft-gallery>` - NFT display
- `<oasis-nft-management>` - NFT operations
- `<oasis-geonft-map>` - Location NFTs
- `<oasis-geonft-management>` - Geo-NFT ops

### Communication
- `<oasis-messaging>` - Chat
- `<oasis-chat-widget>` - Chat widget
- `<oasis-notifications>` - Alerts

## 📖 Basic Usage

```html
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.css">
</head>
<body>
  <!-- Avatar SSO -->
  <oasis-avatar-sso
    providers="holochain,ethereum,solana"
    theme="dark"
  ></oasis-avatar-sso>

  <!-- Karma Management -->
  <oasis-karma-management
    avatar-id="your-avatar-id"
    show-history="true"
  ></oasis-karma-management>

  <!-- NFT Gallery -->
  <oasis-nft-gallery
    collections="my-nfts"
    columns="3"
  ></oasis-nft-gallery>

  <!-- Messaging -->
  <oasis-messaging
    chat-id="global-chat"
    position="bottom-right"
  ></oasis-messaging>

  <script src="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.js"></script>
  <script>
    // Listen for events
    document.querySelector('oasis-avatar-sso').addEventListener('success', (e) => {
      console.log('Logged in:', e.detail);
    });
  </script>
</body>
</html>
```

## 🔧 JavaScript API

```javascript
// Initialize OASIS
const oasis = new OASIS({
  apiEndpoint: 'https://api.oasis.network',
  defaultProvider: 'holochain'
});

// Authentication
const avatar = await oasis.auth.login('holochain');
await oasis.auth.logout();

// Karma
const karma = await oasis.karma.get(avatarId);
await oasis.karma.add(avatarId, 100, 'Quest completed');

// NFTs
const nfts = await oasis.nft.getAll(avatarId);
await oasis.nft.mint(nftData);
await oasis.nft.transfer(nftId, toAddress);

// Messaging
await oasis.messaging.send(chatId, message);
oasis.messaging.onMessage((msg) => {
  console.log('New message:', msg);
});

// Data
await oasis.data.save(key, value);
const data = await oasis.data.load(key);
```

## 🎨 Customization

```javascript
// Custom theme
OASIS.setTheme({
  primaryColor: '#00bcd4',
  secondaryColor: '#ff4081',
  backgroundColor: '#000000',
  textColor: '#ffffff'
});

// Custom styling via CSS
.oasis-sso {
  --oasis-primary: #00bcd4;
  --oasis-secondary: #ff4081;
  --oasis-radius: 12px;
}
```

## 📚 Documentation

Full docs: https://docs.oasis.network/webui-devkit/vanilla

## 🛠️ Requirements

- Modern browser with Web Components support
- No framework dependencies!

## 📄 License

MIT License

