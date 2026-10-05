# @oasis/web5-star-api-client

Official JavaScript/TypeScript client for the OASIS Web5 STAR API - Gamification, OAPPs, missions, quests, and metaverse functionality.

## 🌟 What is STAR?

STAR (Synergistic Interoperable Unified Reality) is the Web5 gamification and metaverse layer of the OASIS ecosystem. It provides:

- **OAPPs**: Decentralized applications built on OASIS
- **Gamification**: Missions, quests, chapters, and progression systems
- **Holons & Zomes**: Plug-and-play data objects and code modules
- **STARNET**: Decentralized network infrastructure
- **Our World**: The metaverse integration layer

## 🚀 Installation

```bash
npm install @oasis/web5-star-api-client
```

or

```bash
yarn add @oasis/web5-star-api-client
```

## 📖 Quick Start

### Basic Usage

```typescript
import { OASISWeb5STARClient } from '@oasis/web5-star-api-client';

// Initialize the client
const client = new OASISWeb5STARClient({
  apiUrl: 'http://localhost:50564/api',
  debug: true
});

// Ignite STAR
const result = await client.igniteSTAR();
console.log('STAR Status:', result.result);
```

### React Example

```typescript
import { useState, useEffect } from 'react';
import { OASISWeb5STARClient } from '@oasis/web5-star-api-client';

function App() {
  const [client] = useState(() => new OASISWeb5STARClient());
  const [starStatus, setStarStatus] = useState(null);

  useEffect(() => {
    async function initSTAR() {
      await client.igniteSTAR();
      const status = await client.getSTARStatus();
      setStarStatus(status.result);
    }
    initSTAR();
  }, []);

  return (
    <div>
      <h1>STAR Status: {starStatus?.isIgnited ? 'Ignited' : 'Offline'}</h1>
    </div>
  );
}
```

## 🌐 STAR Core Operations

### Ignite STAR

```typescript
const result = await client.igniteSTAR();
```

### Get STAR Status

```typescript
const status = await client.getSTARStatus();
console.log('Ignited:', status.result.isIgnited);
console.log('Active OAPPs:', status.result.oappsRunning);
```

### Light STAR

```typescript
await client.lightSTAR();
```

### Extinguish STAR

```typescript
await client.extinguishSTAR();
```

## 📱 OAPP Management

### Get All OAPPs

```typescript
const oapps = await client.getAllOAPPs();
```

### Create OAPP

```typescript
const newOAPP = await client.createOAPP({
  name: 'My Awesome OAPP',
  description: 'A decentralized app on OASIS',
  category: 'Social',
  version: '1.0.0',
  icon: 'https://example.com/icon.png'
});
```

### Install OAPP

```typescript
await client.installOAPP('oapp-id', 'avatar-id');
```

### Publish OAPP

```typescript
await client.publishOAPP('oapp-id');
```

## 🎮 Gamification

### Missions

```typescript
// Get all missions
const missions = await client.getAllMissions();

// Start a mission
await client.startMission('mission-id', 'avatar-id');

// Complete a mission
await client.completeMission('mission-id', 'avatar-id');
```

### Quests

```typescript
// Get all quests
const quests = await client.getAllQuests();

// Start a quest
await client.startQuest('quest-id', 'avatar-id');

// Update quest progress
await client.updateQuestProgress('quest-id', 'avatar-id', {
  objectiveId: 'obj-1',
  progress: 50
});

// Complete a quest
await client.completeQuest('quest-id', 'avatar-id');
```

### Chapters

```typescript
const chapters = await client.getAllChapters();
const chapter = await client.getChapter('chapter-id');
```

### Create Mission

```typescript
const mission = await client.createMission({
  title: 'First Contact',
  description: 'Make your first connection in the OASIS',
  difficulty: 'easy',
  estimatedTime: 15,
  karmaReward: 100,
  xpReward: 50,
  objectives: [
    {
      description: 'Connect with another avatar',
      type: 'social',
      target: 1,
      current: 0,
      completed: false
    }
  ]
});
```

## 🧩 Holons & Zomes

### Holons (Data Objects)

```typescript
// Get all holons
const holons = await client.getAllHolons();

// Create a holon
const holon = await client.createHolon({
  name: 'User Profile Holon',
  description: 'Complete user profile data structure',
  category: 'User Management',
  type: 'Data Object',
  version: '1.0.0'
});

// Install a holon
await client.updateHolon('holon-id', { isInstalled: true });
```

### Zomes (Code Modules)

```typescript
// Get all zomes
const zomes = await client.getAllZomes();

// Install a zome in an OAPP
await client.installZome('zome-id', 'oapp-id');
```

## 🔌 STAR Plugins

```typescript
// Get all STAR plugins
const plugins = await client.getAllSTARPlugins();

// Install a plugin
await client.installSTARPlugin('plugin-id');

// Uninstall a plugin
await client.uninstallSTARPlugin('plugin-id');
```

## 📚 OAPP Templates

```typescript
// Get all templates
const templates = await client.getAllTemplates();

// Create OAPP from template
const oapp = await client.createOAPPFromTemplate(
  'template-id',
  'My New App',
  { theme: 'dark', features: ['auth', 'social'] }
);
```

## 🌍 STARNET Operations

### Join STARNET

```typescript
await client.joinSTARNET({
  nodeType: 'full',
  bandwidth: 'high'
});
```

### Get STARNET Status

```typescript
const status = await client.getSTARNETStatus();
console.log('Connected nodes:', status.result.nodeCount);
```

### Get STARNET Nodes

```typescript
const nodes = await client.getSTARNETNodes();
```

## 🛠️ Developer Portal

### Get Resources

```typescript
const resources = await client.getDevPortalResources();
```

### Get Statistics

```typescript
const stats = await client.getDevPortalStats();
console.log('Total resources:', stats.result.totalResources);
console.log('Total downloads:', stats.result.totalDownloads);
```

## ⚙️ Configuration

### Advanced Configuration

```typescript
const client = new OASISWeb5STARClient({
  apiUrl: 'https://star-api.oasis.network',
  timeout: 60000,
  debug: true,
  autoRetry: true,
  maxRetries: 5
});
```

### Environment Variables

```bash
STAR_API_URL=http://localhost:50564/api
```

## 🔗 Integration with Web4 API

STAR builds on top of the OASIS Web4 API. Use them together:

```typescript
import { OASISWeb4Client } from '@oasis/web4-api-client';
import { OASISWeb5STARClient } from '@oasis/web5-star-api-client';

const web4Client = new OASISWeb4Client();
const starClient = new OASISWeb5STARClient();

// Authenticate via Web4
const auth = await web4Client.authenticate('holochain');
starClient.setAuthToken(auth.result.token);

// Use STAR features
await starClient.igniteSTAR();
const oapps = await starClient.getAllOAPPs();
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

Example:

```typescript
const result = await client.igniteSTAR();

if (result.isError) {
  console.error('Error:', result.message);
} else {
  console.log('Success:', result.result);
}
```

## 📚 TypeScript Support

Full TypeScript support with complete type definitions:

```typescript
import {
  OASISWeb5STARClient,
  OAPP,
  Mission,
  Quest,
  Holon,
  Zome,
  STARStatus
} from '@oasis/web5-star-api-client';
```

## 🧪 Testing

```bash
npm test
```

## 📄 License

MIT

## 🔗 Links

- [Documentation](https://docs.oasis.network/web5-star-api)
- [Web4 API Client](https://www.npmjs.com/package/@oasis/web4-api-client)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS-STAR)
- [Discord](https://discord.gg/oasis-network)

## 🤝 Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md).
