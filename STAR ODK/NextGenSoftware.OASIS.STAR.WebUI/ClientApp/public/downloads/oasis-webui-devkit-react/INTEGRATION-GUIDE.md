# OASIS Web UI Dev Kit - React Integration Guide

Complete guide to integrating OASIS components into your React application.

## 📋 Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Provider Setup](#provider-setup)
4. [Component Examples](#component-examples)
5. [Customization](#customization)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

## Installation

```bash
npm install @oasis/webui-devkit-react
# or
yarn add @oasis/webui-devkit-react
```

### Peer Dependencies

```bash
npm install react react-dom framer-motion react-query axios
```

## Quick Start

### 1. Wrap Your App with OASISProvider

```tsx
// App.tsx
import { OASISProvider } from '@oasis/webui-devkit-react';

function App() {
  return (
    <OASISProvider
      config={{
        apiEndpoint: 'https://api.oasis.network',
        defaultProvider: 'holochain',
        theme: 'dark'
      }}
    >
      <YourApp />
    </OASISProvider>
  );
}
```

### 2. Use Components

```tsx
import { AvatarSSO, KarmaManagement } from '@oasis/webui-devkit-react';

function Dashboard() {
  const [avatarId, setAvatarId] = useState('');

  return (
    <div>
      <AvatarSSO 
        onSuccess={(avatar) => setAvatarId(avatar.id)}
      />
      {avatarId && <KarmaManagement avatarId={avatarId} />}
    </div>
  );
}
```

## Provider Setup

### Basic Configuration

```tsx
<OASISProvider
  config={{
    apiEndpoint: process.env.REACT_APP_OASIS_API,
    defaultProvider: 'holochain',
    theme: 'dark',
    enableAnalytics: true,
    debug: process.env.NODE_ENV === 'development'
  }}
>
```

### Advanced Configuration

```tsx
<OASISProvider
  config={{
    apiEndpoint: 'https://api.oasis.network',
    providers: {
      holochain: { priority: 1, enabled: true },
      ethereum: { priority: 2, enabled: true },
      solana: { priority: 3, enabled: false }
    },
    cache: {
      enabled: true,
      ttl: 3600000 // 1 hour
    },
    retry: {
      attempts: 3,
      delay: 1000
    }
  }}
>
```

## Component Examples

### Authentication Flow

```tsx
import { AvatarSSO, AvatarDetail } from '@oasis/webui-devkit-react';

function AuthFlow() {
  const [avatar, setAvatar] = useState(null);

  const handleLogin = (avatarData) => {
    setAvatar(avatarData);
    localStorage.setItem('oasis_session', avatarData.sessionToken);
  };

  const handleLogout = () => {
    setAvatar(null);
    localStorage.removeItem('oasis_session');
  };

  return (
    <>
      {!avatar ? (
        <AvatarSSO
          providers={['holochain', 'ethereum', 'solana']}
          onSuccess={handleLogin}
          onError={(error) => console.error('Login failed:', error)}
          theme="dark"
        />
      ) : (
        <>
          <AvatarDetail 
            avatar={avatar}
            onUpdate={(updated) => setAvatar(updated)}
          />
          <button onClick={handleLogout}>Logout</button>
        </>
      )}
    </>
  );
}
```

### Karma System

```tsx
import { KarmaManagement, KarmaLeaderboard } from '@oasis/webui-devkit-react';

function KarmaPage({ avatarId }) {
  return (
    <div className="karma-container">
      <KarmaManagement
        avatarId={avatarId}
        theme="dark"
        showHistory={true}
        showLeaderboard={false}
        onKarmaChange={(newKarma) => {
          console.log('Karma updated:', newKarma);
        }}
      />

      <KarmaLeaderboard
        limit={10}
        timeRange="week"
        highlightCurrentUser={true}
      />
    </div>
  );
}
```

### NFT Gallery with Management

```tsx
import { NFTGallery, NFTManagement } from '@oasis/webui-devkit-react';

function NFTPage({ avatarId }) {
  const [selectedNFT, setSelectedNFT] = useState(null);

  return (
    <div>
      <NFTGallery
        avatarId={avatarId}
        columns={3}
        onSelect={setSelectedNFT}
        enableFilters={true}
        sortBy="date"
      />

      {selectedNFT && (
        <NFTManagement
          nft={selectedNFT}
          enableMinting={true}
          enableTransfer={true}
          onTransferComplete={() => {
            alert('NFT transferred successfully!');
            setSelectedNFT(null);
          }}
        />
      )}
    </div>
  );
}
```

### Geo-NFT Map

```tsx
import { GeoNFTMap, GeoNFTManagement } from '@oasis/webui-devkit-react';

function GeoNFTPage({ avatarId }) {
  const [selectedLocation, setSelectedLocation] = useState(null);

  return (
    <div style={{ height: '600px' }}>
      <GeoNFTMap
        avatarId={avatarId}
        center={{ lat: 51.5074, lng: -0.1278 }} // London
        zoom={12}
        enableCreation={true}
        onLocationSelect={setSelectedLocation}
        theme="dark"
      />

      {selectedLocation && (
        <GeoNFTManagement
          location={selectedLocation}
          avatarId={avatarId}
          onMint={(geoNFT) => {
            console.log('Geo-NFT created:', geoNFT);
          }}
        />
      )}
    </div>
  );
}
```

### Real-time Messaging

```tsx
import { Messaging, ChatWidget } from '@oasis/webui-devkit-react';

function ChatPage({ avatarId }) {
  return (
    <>
      {/* Inline messaging */}
      <Messaging
        chatId="global-chat"
        avatarId={avatarId}
        position="inline"
        enableEmojis={true}
        enableFileSharing={true}
        theme="dark"
      />

      {/* Floating chat widget */}
      <ChatWidget
        chatId="support-chat"
        avatarId={avatarId}
        position="bottom-right"
        defaultOpen={false}
        enableNotifications={true}
      />
    </>
  );
}
```

### Data Management

```tsx
import { DataManagement, ProviderManagement } from '@oasis/webui-devkit-react';

function DataPage({ avatarId }) {
  return (
    <div className="data-page">
      <ProviderManagement
        showStatus={true}
        onProviderChange={(provider) => {
          console.log('Switched to:', provider);
        }}
      />

      <DataManagement
        avatarId={avatarId}
        enableExport={true}
        enableImport={true}
        onDataChange={(data) => {
          console.log('Data updated:', data);
        }}
      />
    </div>
  );
}
```

## Customization

### Theme Customization

```tsx
import { OASISProvider } from '@oasis/webui-devkit-react';

const customTheme = {
  colors: {
    primary: '#00bcd4',
    secondary: '#ff4081',
    background: '#1a1a1a',
    surface: '#2a2a2a',
    text: '#ffffff',
    textSecondary: '#b0b0b0'
  },
  fonts: {
    body: 'Inter, sans-serif',
    heading: 'Poppins, sans-serif',
    mono: 'Fira Code, monospace'
  },
  spacing: {
    unit: 8
  },
  borderRadius: {
    small: 4,
    medium: 8,
    large: 12
  }
};

<OASISProvider config={{ theme: customTheme }}>
```

### Custom Styles

```tsx
// Using inline styles
<KarmaManagement
  customStyles={{
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '20px',
    padding: '2rem'
  }}
/>

// Using CSS classes
<AvatarSSO
  className="my-custom-sso"
  theme="dark"
/>
```

### CSS Variables

```css
:root {
  --oasis-primary: #00bcd4;
  --oasis-secondary: #ff4081;
  --oasis-radius: 12px;
  --oasis-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}
```

## Best Practices

### 1. Error Handling

```tsx
import { AvatarSSO } from '@oasis/webui-devkit-react';

function Login() {
  const handleError = (error) => {
    if (error.code === 'PROVIDER_UNAVAILABLE') {
      alert('Provider is currently unavailable. Please try another.');
    } else {
      console.error('Login error:', error);
      alert('Login failed. Please try again.');
    }
  };

  return (
    <AvatarSSO
      onSuccess={handleLogin}
      onError={handleError}
    />
  );
}
```

### 2. Loading States

```tsx
import { KarmaManagement } from '@oasis/webui-devkit-react';
import { Suspense } from 'react';

function KarmaSection({ avatarId }) {
  return (
    <Suspense fallback={<LoadingSpinner />}>
      <KarmaManagement avatarId={avatarId} />
    </Suspense>
  );
}
```

### 3. Performance Optimization

```tsx
import { memo } from 'react';
import { NFTGallery } from '@oasis/webui-devkit-react';

const MemoizedNFTGallery = memo(NFTGallery);

function NFTPage({ avatarId }) {
  return <MemoizedNFTGallery avatarId={avatarId} />;
}
```

## Troubleshooting

### Common Issues

**Issue: Components not rendering**
```tsx
// Make sure OASISProvider wraps your app
<OASISProvider config={{ apiEndpoint: '...' }}>
  <App />
</OASISProvider>
```

**Issue: API calls failing**
```tsx
// Check CORS settings and API endpoint
config={{
  apiEndpoint: 'https://api.oasis.network',
  headers: {
    'Content-Type': 'application/json'
  }
}}
```

**Issue: Styling not applied**
```tsx
// Import CSS
import '@oasis/webui-devkit-react/dist/style.css';
```

### Debug Mode

```tsx
<OASISProvider
  config={{
    debug: true, // Enables console logging
    logLevel: 'verbose' // 'error' | 'warn' | 'info' | 'verbose'
  }}
>
```

## Support

- Documentation: https://docs.oasis.network/webui-devkit/react
- GitHub: https://github.com/oasis-network/webui-devkit-react
- Discord: https://discord.gg/oasis-network
- Email: support@oasis.network

