/**
 * Data Demo Data
 * Demo data for data operations (NFTs, GeoNFTs, Missions, etc.)
 */

export const dataDemoData = {
  // NFT Operations
  nft: {
    create: (payload: any) => ({
      id: 'demo-nft-1',
      name: payload.name || 'Demo NFT',
      description: payload.description || 'A demo NFT',
      imageUrl: payload.imageUrl || 'https://demo.com/image.png',
      tokenId: '1',
      contractAddress: '0x1234567890abcdef',
      owner: 'demo-avatar-1',
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    mint: (payload: any) => ({
      id: 'demo-nft-minted',
      name: payload.name || 'Minted NFT',
      tokenId: '2',
      contractAddress: '0x1234567890abcdef',
      owner: 'demo-avatar-1',
      mintedOn: new Date().toISOString(),
    }),

    transfer: (id: string, toAddress: string) => ({
      id,
      owner: toAddress,
      transferredOn: new Date().toISOString(),
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-nft-1',
        name: 'Cool NFT',
        description: 'A really cool NFT',
        imageUrl: 'https://demo.com/cool.png',
        tokenId: '1',
        owner: 'demo-avatar-1',
        value: '0.1 ETH',
      },
      {
        id: 'demo-nft-2',
        name: 'Awesome NFT',
        description: 'An awesome NFT',
        imageUrl: 'https://demo.com/awesome.png',
        tokenId: '2',
        owner: 'demo-avatar-2',
        value: '0.2 ETH',
      },
    ],
  },

  // GeoNFT Operations
  geoNft: {
    create: (payload: any) => ({
      id: 'demo-geo-nft-1',
      name: payload.name || 'Demo GeoNFT',
      description: payload.description || 'A demo GeoNFT',
      latitude: payload.latitude || 40.7128,
      longitude: payload.longitude || -74.0060,
      imageUrl: payload.imageUrl || 'https://demo.com/geo.png',
      tokenId: '1',
      contractAddress: '0x1234567890abcdef',
      owner: 'demo-avatar-1',
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    mint: (payload: any) => ({
      id: 'demo-geo-nft-minted',
      name: payload.name || 'Minted GeoNFT',
      latitude: payload.latitude || 40.7128,
      longitude: payload.longitude || -74.0060,
      tokenId: '2',
      contractAddress: '0x1234567890abcdef',
      owner: 'demo-avatar-1',
      mintedOn: new Date().toISOString(),
    }),

    place: (id: string, latitude: number, longitude: number) => ({
      id,
      latitude,
      longitude,
      placedOn: new Date().toISOString(),
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-geo-nft-1',
        name: 'NYC GeoNFT',
        description: 'A GeoNFT in New York City',
        latitude: 40.7128,
        longitude: -74.0060,
        imageUrl: 'https://demo.com/nyc.png',
        tokenId: '1',
        owner: 'demo-avatar-1',
        value: '0.5 ETH',
      },
      {
        id: 'demo-geo-nft-2',
        name: 'LA GeoNFT',
        description: 'A GeoNFT in Los Angeles',
        latitude: 34.0522,
        longitude: -118.2437,
        imageUrl: 'https://demo.com/la.png',
        tokenId: '2',
        owner: 'demo-avatar-2',
        value: '0.3 ETH',
      },
    ],
  },

  // Mission Operations
  mission: {
    create: (payload: any) => ({
      id: 'demo-mission-1',
      name: payload.name || 'Demo Mission',
      description: payload.description || 'A demo mission',
      type: payload.type || 'Quest',
      difficulty: payload.difficulty || 'Medium',
      rewards: payload.rewards || ['100 XP', '50 Coins'],
      isActive: true,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    start: (id: string) => ({
      id,
      isActive: true,
      startedOn: new Date().toISOString(),
    }),

    complete: (id: string) => ({
      id,
      isCompleted: true,
      completedOn: new Date().toISOString(),
      rewards: ['100 XP', '50 Coins'],
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-mission-1',
        name: 'Find the Lost Artifact',
        description: 'Search for the ancient artifact',
        type: 'Quest',
        difficulty: 'Hard',
        rewards: ['500 XP', 'Rare Item'],
        isActive: true,
      },
      {
        id: 'demo-mission-2',
        name: 'Defeat the Dragon',
        description: 'Battle the mighty dragon',
        type: 'Combat',
        difficulty: 'Extreme',
        rewards: ['1000 XP', 'Dragon Scale'],
        isActive: false,
      },
    ],
  },

  // Quest Operations
  quest: {
    create: (payload: any) => ({
      id: 'demo-quest-1',
      name: payload.name || 'Demo Quest',
      description: payload.description || 'A demo quest',
      type: payload.type || 'Main',
      difficulty: payload.difficulty || 'Medium',
      rewards: payload.rewards || ['200 XP', '100 Coins'],
      isActive: true,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    start: (id: string) => ({
      id,
      isActive: true,
      startedOn: new Date().toISOString(),
    }),

    complete: (id: string) => ({
      id,
      isCompleted: true,
      completedOn: new Date().toISOString(),
      rewards: ['200 XP', '100 Coins'],
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-quest-1',
        name: 'The Great Adventure',
        description: 'Embark on an epic adventure',
        type: 'Main',
        difficulty: 'Medium',
        rewards: ['500 XP', 'Adventure Badge'],
        isActive: true,
      },
      {
        id: 'demo-quest-2',
        name: 'Side Quest: Help the Villager',
        description: 'Help a villager in need',
        type: 'Side',
        difficulty: 'Easy',
        rewards: ['100 XP', 'Villager Thanks'],
        isActive: false,
      },
    ],
  },

  // Chapter Operations
  chapter: {
    create: (payload: any) => ({
      id: 'demo-chapter-1',
      name: payload.name || 'Demo Chapter',
      description: payload.description || 'A demo chapter',
      questId: payload.questId || 'demo-quest-1',
      order: payload.order || 1,
      isActive: true,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    start: (id: string) => ({
      id,
      isActive: true,
      startedOn: new Date().toISOString(),
    }),

    complete: (id: string) => ({
      id,
      isCompleted: true,
      completedOn: new Date().toISOString(),
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-chapter-1',
        name: 'Chapter 1: The Beginning',
        description: 'The start of the adventure',
        questId: 'demo-quest-1',
        order: 1,
        isActive: true,
      },
      {
        id: 'demo-chapter-2',
        name: 'Chapter 2: The Journey',
        description: 'The journey continues',
        questId: 'demo-quest-1',
        order: 2,
        isActive: false,
      },
    ],
  },

  // Inventory Item Operations
  inventoryItem: {
    create: (payload: any) => ({
      id: 'demo-inventory-1',
      name: payload.name || 'Demo Item',
      description: payload.description || 'A demo inventory item',
      type: payload.type || 'Weapon',
      rarity: payload.rarity || 'Common',
      value: payload.value || 100,
      owner: 'demo-avatar-1',
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    transfer: (id: string, toAvatarId: string) => ({
      id,
      owner: toAvatarId,
      transferredOn: new Date().toISOString(),
    }),

    search: (searchTerm: string) => [
      {
        id: 'demo-inventory-1',
        name: 'Magic Sword',
        description: 'A powerful magic sword',
        type: 'Weapon',
        rarity: 'Rare',
        value: 500,
        owner: 'demo-avatar-1',
      },
      {
        id: 'demo-inventory-2',
        name: 'Health Potion',
        description: 'Restores health',
        type: 'Consumable',
        rarity: 'Common',
        value: 50,
        owner: 'demo-avatar-1',
      },
    ],
  },
};
