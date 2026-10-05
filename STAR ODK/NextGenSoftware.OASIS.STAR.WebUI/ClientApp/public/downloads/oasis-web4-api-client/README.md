# @oasis/web4-api-client

Official JavaScript/TypeScript client for the OASIS Web4 API - Decentralized avatar management, karma system, NFTs, and cross-provider data storage.

## 🚀 Installation

```bash
npm install @oasis/web4-api-client
```

or

```bash
yarn add @oasis/web4-api-client
```

## 📖 Quick Start

### Basic Usage

```typescript
import { OASISWeb4Client } from '@oasis/web4-api-client';

// Initialize the client
const client = new OASISWeb4Client({
  apiUrl: 'http://localhost:5000/api',
  debug: true
});

// Authenticate
const authResult = await client.authenticate('holochain', {
  username: 'user@example.com',
  password: 'password123'
});

if (!authResult.isError) {
  console.log('Logged in:', authResult.result.avatar);
}
```

### React Example

```typescript
import { useState, useEffect } from 'react';
import { OASISWeb4Client } from '@oasis/web4-api-client';

function App() {
  const [client] = useState(() => new OASISWeb4Client());
  const [avatar, setAvatar] = useState(null);

  const login = async (provider: string) => {
    const result = await client.authenticate(provider);
    if (!result.isError) {
      setAvatar(result.result.avatar);
    }
  };

  return (
    <div>
      <button onClick={() => login('holochain')}>Login</button>
      {avatar && <p>Welcome, {avatar.username}!</p>}
    </div>
  );
}
```

## 🔑 Authentication

### Authenticate with Provider

```typescript
const result = await client.authenticateWithProvider('ethereum');
```

### Set Auth Token

```typescript
client.setAuthToken('your-jwt-token');
```

### Logout

```typescript
await client.logout();
```

## 👤 Avatar Management

### Get Avatar

```typescript
// By ID
const avatar = await client.getAvatar('avatar-id');

// By username
const avatar = await client.getAvatarByUsername('john_doe');

// By email
const avatar = await client.getAvatarByEmail('john@example.com');
```

### Create Avatar

```typescript
const newAvatar = await client.createAvatar({
  username: 'john_doe',
  email: 'john@example.com',
  password: 'secure-password',
  firstName: 'John',
  lastName: 'Doe',
  acceptTerms: true
});
```

### Update Avatar

```typescript
const updated = await client.updateAvatar('avatar-id', {
  bio: 'Web3 enthusiast',
  image: 'https://example.com/avatar.jpg'
});
```

### Search Avatars

```typescript
const results = await client.searchAvatars('john');
```

## 🏆 Karma System

### Get Karma

```typescript
const karma = await client.getKarma('avatar-id');
console.log('Total karma:', karma.result.total);
console.log('Rank:', karma.result.rank);
```

### Add Karma

```typescript
await client.addKarma('avatar-id', {
  amount: 100,
  reason: 'Completed quest',
  karmaType: 'QuestCompleted'
});
```

### Get Karma History

```typescript
const history = await client.getKarmaHistory('avatar-id', 50);
```

### Get Leaderboard

```typescript
const leaderboard = await client.getKarmaLeaderboard('month', 100);
```

## 🎨 NFT Operations

### Get NFTs

```typescript
const nfts = await client.getNFTs('avatar-id');
```

### Mint NFT

```typescript
const nft = await client.mintNFT('avatar-id', {
  name: 'My Awesome NFT',
  description: 'A unique digital asset',
  imageUrl: 'https://example.com/nft.png',
  collection: 'OASIS Collection',
  price: 1000,
  metadata: {
    attributes: [
      { trait_type: 'Rarity', value: 'Legendary' }
    ]
  }
});
```

### Transfer NFT

```typescript
await client.transferNFT('nft-id', 'recipient-avatar-id');
```

### Burn NFT

```typescript
await client.burnNFT('nft-id');
```

## 💾 Data Storage

### Save Data

```typescript
await client.saveData('avatar-id', 'preferences', {
  theme: 'dark',
  notifications: true
});
```

### Get Data

```typescript
// Get all data
const allData = await client.getData('avatar-id');

// Get specific key
const prefs = await client.getData('avatar-id', 'preferences');
```

### Delete Data

```typescript
await client.deleteData('avatar-id', 'preferences');
```

## 🔌 Provider Management

### Get Available Providers

```typescript
const providers = await client.getAvailableProviders();
```

### Get Current Provider

```typescript
const current = await client.getCurrentProvider();
```

### Switch Provider

```typescript
await client.switchProvider('ipfs');
```

## 💬 Messaging

### Get Chat Messages

```typescript
const messages = await client.getChatMessages('chat-id', 100);
```

### Send Message

```typescript
await client.sendMessage('chat-id', 'avatar-id', 'Hello, world!');
```

## 🌐 Social Features

### Get Social Feed

```typescript
// Global feed
const feed = await client.getSocialFeed('global');

// Friends feed
const friendsFeed = await client.getSocialFeed('friends', 'avatar-id');
```

### Get Friends

```typescript
const friends = await client.getFriends('avatar-id');
```

### Add Friend

```typescript
await client.addFriend('avatar-id', 'friend-avatar-id');
```

### Like Post

```typescript
await client.likePost('post-id');
```

### Get Achievements

```typescript
const achievements = await client.getAchievements('avatar-id');
```

## ⚙️ Configuration

### Advanced Configuration

```typescript
const client = new OASISWeb4Client({
  apiUrl: 'https://api.oasis.network',
  timeout: 60000,           // Request timeout in ms
  debug: true,              // Enable debug logging
  autoRetry: true,          // Auto-retry failed requests
  maxRetries: 5             // Maximum retry attempts
});
```

### Environment Variables

```bash
OASIS_WEB4_API_URL=http://localhost:5000/api
```

## 🔒 Error Handling

All methods return an `OASISResult` object:

```typescript
interface OASISResult<T> {
  isError: boolean;
  message: string;
  result: T | null;
  isSaved?: boolean;
}
```

Example error handling:

```typescript
const result = await client.getAvatar('avatar-id');

if (result.isError) {
  console.error('Error:', result.message);
} else {
  console.log('Avatar:', result.result);
}
```

## 📚 TypeScript Support

Full TypeScript support with complete type definitions:

```typescript
import { OASISWeb4Client, Avatar, Karma, NFT } from '@oasis/web4-api-client';

const client = new OASISWeb4Client();

// Types are automatically inferred
const avatar: Avatar = result.result;
```

## 🧪 Testing

```bash
npm test
```

## 📄 License

MIT

## 🔗 Links

- [Documentation](https://docs.oasis.network/web4-api)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS-API)
- [Discord](https://discord.gg/oasis-network)

## 🤝 Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md).
