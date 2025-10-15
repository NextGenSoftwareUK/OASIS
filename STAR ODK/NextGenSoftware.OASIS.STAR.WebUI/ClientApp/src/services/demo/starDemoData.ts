/**
 * STAR Core Demo Data
 * Demo data for core STAR operations (ignite, extinguish, status, etc.)
 */

export const starDemoData = {
  // Core STAR Operations
  ignite: () => ({
    ignited: true,
    timestamp: new Date().toISOString(),
    version: '3.0.0',
  }),

  extinguish: () => ({
    extinguished: true,
    timestamp: new Date().toISOString(),
  }),

  status: () => ({
    isIgnited: true,
    version: '3.0.0',
    status: 'running',
    lastUpdated: new Date(),
  }),

  // Avatar Operations
  beamIn: (username: string, password: string) => ({
    id: 'demo-avatar-1',
    username,
    email: `${username}@demo.com`,
    firstName: 'Demo',
    lastName: 'User',
    isAuthenticated: true,
    lastLogin: new Date().toISOString(),
  }),

  createAvatar: (data: any) => ({
    id: 'demo-avatar-new',
    username: data.username,
    email: data.email,
    firstName: data.firstName || 'New',
    lastName: data.lastName || 'User',
    isAuthenticated: true,
    createdOn: new Date().toISOString(),
  }),

  getCurrentAvatar: () => ({
    id: 'demo-avatar-1',
    username: 'demo-user',
    email: 'demo@demo.com',
    firstName: 'Demo',
    lastName: 'User',
    isAuthenticated: true,
    lastLogin: new Date().toISOString(),
  }),

  getAllAvatars: () => [
    {
      id: 'demo-avatar-1',
      username: 'demo-user',
      email: 'demo@demo.com',
      firstName: 'Demo',
      lastName: 'User',
      isAuthenticated: true,
    },
    {
      id: 'demo-avatar-2',
      username: 'test-user',
      email: 'test@demo.com',
      firstName: 'Test',
      lastName: 'User',
      isAuthenticated: false,
    },
  ],

  // Karma System
  getAllKarma: () => [
    {
      id: 'demo-karma-1',
      avatarId: 'demo-avatar-1',
      karma: 150,
      level: 5,
      title: 'Explorer',
      description: 'Has explored multiple worlds',
    },
    {
      id: 'demo-karma-2',
      avatarId: 'demo-avatar-2',
      karma: 75,
      level: 3,
      title: 'Novice',
      description: 'Just starting their journey',
    },
  ],

  getKarmaForAvatar: (avatarId: string) => ({
    id: 'demo-karma-1',
    avatarId,
    karma: 150,
    level: 5,
    title: 'Explorer',
    description: 'Has explored multiple worlds',
    achievements: [
      { id: 'ach-1', name: 'First Quest', description: 'Completed first quest', earnedOn: '2024-01-15' },
      { id: 'ach-2', name: 'Explorer', description: 'Visited 5 different worlds', earnedOn: '2024-01-20' },
    ],
  }),

  // System Stats
  getDashboardData: () => ({
    totalAvatars: 1250,
    activeAvatars: 890,
    totalOAPPs: 45,
    publishedOAPPs: 32,
    totalTemplates: 18,
    totalRuntimes: 8,
    totalLibraries: 25,
    systemHealth: 'excellent',
    uptime: '99.9%',
    lastUpdate: new Date().toISOString(),
  }),

  getDevPortalStats: () => ({
    totalDevelopers: 156,
    activeProjects: 23,
    totalDownloads: 12500,
    totalInstalls: 8900,
    averageRating: 4.7,
    topCategories: ['Games', 'Tools', 'Educational'],
  }),

  getDevPortalResources: () => ({
    documentation: {
      apiDocs: 'https://docs.oasis.network',
      tutorials: 'https://tutorials.oasis.network',
      examples: 'https://examples.oasis.network',
    },
    tools: {
      sdk: 'https://github.com/oasis-network/sdk',
      cli: 'https://github.com/oasis-network/cli',
      templates: 'https://github.com/oasis-network/templates',
    },
    community: {
      discord: 'https://discord.gg/oasis',
      forum: 'https://forum.oasis.network',
      github: 'https://github.com/oasis-network',
    },
  }),

  getKarmaLeaderboard: () => ({
    leaderboard: [
      { id: '1', username: 'StarMaster', karma: 2500, level: 15, title: 'Legend' },
      { id: '2', username: 'CosmicExplorer', karma: 2200, level: 14, title: 'Master' },
      { id: '3', username: 'QuantumBuilder', karma: 1900, level: 13, title: 'Expert' },
      { id: '4', username: 'NebulaCreator', karma: 1600, level: 12, title: 'Advanced' },
      { id: '5', username: 'GalaxyWalker', karma: 1300, level: 11, title: 'Intermediate' },
    ],
    totalParticipants: 1250,
    averageKarma: 850,
    lastUpdated: new Date().toISOString(),
  }),

  getOAPPKarmaData: (oappId: string) => ({
    oappId,
    oappName: 'Demo OAPP',
    totalKarma: 450,
    averageRating: 4.6,
    totalRatings: 89,
    karmaHistory: [
      { date: '2024-01-01', karma: 100 },
      { date: '2024-01-15', karma: 150 },
      { date: '2024-02-01', karma: 200 },
      { date: '2024-02-15', karma: 300 },
      { date: '2024-03-01', karma: 450 },
    ],
    topContributors: [
      { username: 'StarMaster', contributions: 45, karma: 120 },
      { username: 'CosmicExplorer', contributions: 32, karma: 85 },
      { username: 'QuantumBuilder', contributions: 28, karma: 70 },
    ],
    recentActivity: [
      { user: 'StarMaster', action: 'rated 5 stars', timestamp: '2024-03-15T10:30:00Z' },
      { user: 'CosmicExplorer', action: 'downloaded', timestamp: '2024-03-15T09:15:00Z' },
      { user: 'QuantumBuilder', action: 'rated 4 stars', timestamp: '2024-03-14T16:45:00Z' },
    ],
  }),

  getAvatarById: (id: string) => ({
    id,
    username: 'demo-user',
    email: 'demo@example.com',
    firstName: 'Demo',
    lastName: 'User',
    isAuthenticated: true,
    lastLogin: new Date().toISOString(),
    avatarType: 'Human',
    level: 5,
    experience: 1250,
    karma: 150,
  }),

  updateAvatar: (id: string, data: any) => ({
    id,
    username: data.username || 'demo-user',
    email: data.email || 'demo@example.com',
    firstName: data.firstName || 'Demo',
    lastName: data.lastName || 'User',
    isAuthenticated: true,
    lastLogin: new Date().toISOString(),
    avatarType: data.avatarType || 'Human',
    level: data.level || 5,
    experience: data.experience || 1250,
    karma: data.karma || 150,
  }),

  getAvatarSessions: (id: string) => [
    {
      id: 'session-1',
      avatarId: id,
      device: 'Desktop',
      browser: 'Chrome',
      ipAddress: '192.168.1.100',
      location: 'New York, NY',
      loginTime: '2024-03-15T10:30:00Z',
      lastActivity: '2024-03-15T12:45:00Z',
      isActive: true,
    },
    {
      id: 'session-2',
      avatarId: id,
      device: 'Mobile',
      browser: 'Safari',
      ipAddress: '192.168.1.101',
      location: 'New York, NY',
      loginTime: '2024-03-15T08:15:00Z',
      lastActivity: '2024-03-15T09:30:00Z',
      isActive: false,
    },
  ],

  getMyDataFiles: () => [
    {
      id: 'file-1',
      name: 'Avatar Data',
      type: 'JSON',
      size: '2.5 MB',
      lastModified: '2024-03-15T10:30:00Z',
      description: 'Avatar profile and settings data',
    },
    {
      id: 'file-2',
      name: 'Karma History',
      type: 'CSV',
      size: '1.2 MB',
      lastModified: '2024-03-14T15:45:00Z',
      description: 'Complete karma transaction history',
    },
    {
      id: 'file-3',
      name: 'OAPP Configurations',
      type: 'JSON',
      size: '850 KB',
      lastModified: '2024-03-13T09:20:00Z',
      description: 'Installed OAPP configurations and settings',
    },
  ],

  updateSettings: (settings: any) => ({
    ...settings,
    updatedOn: new Date().toISOString(),
    version: '1.0.0',
  }),

  getStoreItems: () => [
    {
      id: 'store-1',
      name: 'Premium OAPP',
      description: 'Advanced OASIS Application with premium features',
      price: 99.99,
      currency: 'USD',
      category: 'OAPPs',
      rating: 4.8,
      downloads: 1250,
      imageUrl: 'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=400&h=300&fit=crop',
    },
    {
      id: 'store-2',
      name: 'Quantum Runtime',
      description: 'High-performance runtime for quantum computing',
      price: 199.99,
      currency: 'USD',
      category: 'Runtimes',
      rating: 4.9,
      downloads: 890,
      imageUrl: 'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=400&h=300&fit=crop',
    },
    {
      id: 'store-3',
      name: 'AI Library',
      description: 'Machine learning library for OASIS applications',
      price: 149.99,
      currency: 'USD',
      category: 'Libraries',
      rating: 4.7,
      downloads: 2100,
      imageUrl: 'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=400&h=300&fit=crop',
    },
  ],
};
