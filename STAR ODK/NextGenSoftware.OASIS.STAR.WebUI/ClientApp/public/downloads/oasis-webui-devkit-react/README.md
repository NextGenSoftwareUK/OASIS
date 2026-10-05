# OASIS Web UI Dev Kit - React

A comprehensive collection of React components for integrating OASIS functionality into your web applications.

## 🚀 Quick Start

```bash
npm install @oasis/webui-devkit-react
# or
yarn add @oasis/webui-devkit-react
```

## 📦 Components Included

### Authentication & User Management
- **AvatarSSO** - Single Sign-On component with multi-provider support
- **AvatarDetail** - Display and edit avatar profile information
- **AvatarCard** - Compact avatar display card

### Data & Storage
- **DataManagement** - CRUD operations for OASIS data
- **ProviderManagement** - Switch between storage providers
- **OASISSettings** - Configure OASIS settings

### Karma & Gamification
- **KarmaManagement** - Display and manage karma points
- **KarmaLeaderboard** - Show top karma earners
- **AchievementsBadges** - Display user achievements

### NFT & Assets
- **NFTGallery** - Display NFT collections
- **NFTManagement** - Mint, transfer, and manage NFTs
- **GeoNFTMap** - Interactive map for location-based NFTs
- **GeoNFTManagement** - Create and manage Geo-NFTs

### Communication
- **Messaging** - Real-time messaging component
- **ChatWidget** - Embeddable chat widget
- **Notifications** - Toast and notification system

### Social & Community
- **SocialFeed** - Activity feed component
- **FriendsList** - Display and manage connections
- **GroupManagement** - Create and manage groups

## 🎨 Customization

All components are fully customizable with:
- **Theme Support** - Light/Dark mode + custom themes
- **CSS Modules** - Scoped styling
- **Style Props** - Inline customization
- **Custom Renders** - Override default rendering

## 📖 Basic Usage

```tsx
import { 
  AvatarSSO, 
  KarmaManagement, 
  NFTGallery,
  Messaging 
} from '@oasis/webui-devkit-react';

function App() {
  return (
    <div>
      <AvatarSSO
        providers={['holochain', 'ethereum', 'solana']}
        onSuccess={(avatar) => console.log('Logged in:', avatar)}
      />
      
      <KarmaManagement 
        avatarId="your-avatar-id"
        theme="dark"
      />
      
      <NFTGallery 
        collections={['my-nfts']}
        columns={3}
      />
      
      <Messaging
        chatId="global-chat"
        position="bottom-right"
      />
    </div>
  );
}
```

## 🔧 Advanced Configuration

```tsx
import { OASISProvider } from '@oasis/webui-devkit-react';

function App() {
  return (
    <OASISProvider
      config={{
        apiEndpoint: 'https://api.oasis.network',
        defaultProvider: 'holochain',
        theme: 'dark',
        enableAnalytics: true
      }}
    >
      {/* Your components here */}
    </OASISProvider>
  );
}
```

## 📚 Documentation

Full documentation available at: https://docs.oasis.network/webui-devkit/react

## 🛠️ Requirements

- React 18.0+
- TypeScript 4.9+ (optional but recommended)
- Node.js 16+

## 📄 License

MIT License - Use freely in your projects!

