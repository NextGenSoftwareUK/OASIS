# OASIS Web UI Dev Kit - Vue 3

Modern Vue 3 composition API component library for OASIS integration.

## 🚀 Quick Start

```bash
npm install @oasis/webui-devkit-vue
# or
yarn add @oasis/webui-devkit-vue
```

## 📦 Components Included

### Authentication & User Management
- **AvatarSSO** - Multi-provider authentication
- **AvatarDetail** - Profile management
- **AvatarCard** - Compact display

### Data & Storage
- **DataManagement** - CRUD operations
- **ProviderManagement** - Provider switching
- **OASISSettings** - Settings panel

### Karma & Gamification
- **KarmaManagement** - Karma system
- **KarmaLeaderboard** - Rankings
- **AchievementsBadges** - Achievements

### NFT & Assets
- **NFTGallery** - Collection display
- **NFTManagement** - NFT operations
- **GeoNFTMap** - Location NFTs
- **GeoNFTManagement** - Geo-NFT ops

### Communication
- **Messaging** - Real-time chat
- **ChatWidget** - Chat widget
- **Notifications** - Alerts

### Social & Community
- **SocialFeed** - Activity feed
- **FriendsList** - Connections
- **GroupManagement** - Groups

## 📖 Basic Usage

```vue
<script setup>
import { 
  AvatarSSO, 
  KarmaManagement, 
  NFTGallery,
  Messaging 
} from '@oasis/webui-devkit-vue';
import { ref } from 'vue';

const currentAvatarId = ref('');

const handleLogin = (avatar) => {
  currentAvatarId.value = avatar.id;
  console.log('Logged in:', avatar);
};
</script>

<template>
  <div>
    <AvatarSSO
      :providers="['holochain', 'ethereum', 'solana']"
      @success="handleLogin"
    />
    
    <KarmaManagement 
      :avatar-id="currentAvatarId"
      theme="dark"
    />
    
    <NFTGallery 
      :collections="['my-nfts']"
      :columns="3"
    />
    
    <Messaging
      chat-id="global-chat"
      position="bottom-right"
    />
  </div>
</template>
```

## 🎨 Theming

```typescript
// main.ts
import { createOASIS } from '@oasis/webui-devkit-vue';

app.use(createOASIS({
  apiEndpoint: 'https://api.oasis.network',
  defaultProvider: 'holochain',
  theme: {
    primary: '#00bcd4',
    secondary: '#ff4081',
    mode: 'dark'
  }
}));
```

## 🔧 Composables

```vue
<script setup>
import { useOASIS, useKarma, useNFTs } from '@oasis/webui-devkit-vue';

const { avatar, login, logout } = useOASIS();
const { karma, addKarma, history } = useKarma(avatar.value?.id);
const { nfts, mint, transfer } = useNFTs(avatar.value?.id);
</script>
```

## 📚 Documentation

Full docs: https://docs.oasis.network/webui-devkit/vue

## 🛠️ Requirements

- Vue 3.3+
- TypeScript 5.0+ (optional)

## 📄 License

MIT License

