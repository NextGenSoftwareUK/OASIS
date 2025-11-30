export const config = {
  // OASIS API Configuration
  api: {
    baseUrl: process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://localhost:5000',
    version: process.env.NEXT_PUBLIC_OASIS_API_VERSION || 'v1',
    timeout: 30000, // 30 seconds
  },
  
  // Demo user configuration
  demo: {
    userId: '12345678-1234-1234-1234-123456789012',
    username: 'demo_user',
    email: 'demo@oasis.com',
  },

  bridge: {
    apiUrl: process.env.NEXT_PUBLIC_BRIDGE_API_URL || 'https://api.qstreetrwa.com/api/v1',
  },
  
  // UI Configuration
  ui: {
    defaultTheme: 'dark',
    refreshInterval: 30000, // 30 seconds
    maxWalletsPerPage: 20,
  },
  
  // Supported blockchain networks
  networks: {
    ethereum: {
      name: 'Ethereum',
      symbol: 'ETH',
      decimals: 18,
      color: '#627EEA',
    },
    solana: {
      name: 'Solana',
      symbol: 'SOL',
      decimals: 9,
      color: '#9945FF',
    },
    polygon: {
      name: 'Polygon',
      symbol: 'MATIC',
      decimals: 18,
      color: '#8247E5',
    },
    arbitrum: {
      name: 'Arbitrum',
      symbol: 'ARB',
      decimals: 18,
      color: '#28A0F0',
    },
    starknet: {
      name: 'Starknet',
      symbol: 'STRK',
      decimals: 18,
      color: '#8C7BFF',
    },
    zcash: {
      name: 'Zcash',
      symbol: 'ZEC',
      decimals: 8,
      color: '#00ff88',
    },
    aztec: {
      name: 'Aztec',
      symbol: 'AZTEC',
      decimals: 18,
      color: '#60a5fa',
    },
    miden: {
      name: 'Miden',
      symbol: 'MIDEN',
      decimals: 18,
      color: '#8b5cf6',
    },
  },
}; 