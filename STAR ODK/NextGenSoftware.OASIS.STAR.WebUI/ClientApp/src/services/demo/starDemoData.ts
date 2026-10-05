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

  // System Stats - structured to match Dashboard.tsx expectations
  getDashboardData: () => ({
    overview: {
      totalUsers: 2547891,
      activeUsers: 892456,
      totalKarma: 12500000,
      systemHealth: 98.5,
      uptime: 99.9,
      transactions: 4567892,
      growthRate: 12.5,
      userSatisfaction: 4.8,
    },
    metrics: {
      oapps: { total: 1250, active: 892, growth: 8.2 },
      nfts: { total: 45678, active: 23456, growth: 15.3 },
      avatars: { total: 892456, active: 456789, growth: 22.1 },
      runtimes: { total: 234, active: 189, growth: 5.7 },
      libraries: { total: 567, active: 456, growth: 12.8 },
      templates: { total: 1234, active: 987, growth: 18.9 },
      celestialBodies: { total: 4567, active: 3456, growth: 7.4 },
      celestialSpaces: { total: 234, active: 189, growth: 9.2 },
      quests: { total: 1234, active: 567, growth: 14.6 },
      chapters: { total: 2345, active: 1234, growth: 11.3 },
      inventory: { total: 45678, active: 23456, growth: 16.7 },
      plugins: { total: 234, active: 189, growth: 6.9 },
      storeItems: { total: 1234, active: 987, growth: 13.2 },
    },
    recentActivity: [
      { id: 1, type: 'user', action: 'New user registered', user: 'John Doe', time: '2 minutes ago', status: 'success' },
      { id: 2, type: 'oapp', action: 'OAPP published', user: 'Sarah Wilson', time: '5 minutes ago', status: 'success' },
      { id: 3, type: 'runtime', action: 'Runtime activated', user: 'Mike Chen', time: '8 minutes ago', status: 'success' },
      { id: 4, type: 'template', action: 'Template downloaded', user: 'Emma Davis', time: '12 minutes ago', status: 'success' },
      { id: 5, type: 'store', action: 'Asset purchased', user: 'Alex Brown', time: '15 minutes ago', status: 'success' },
    ],
    topOAPPs: [
      { id: '1', name: 'Cosmic Explorer', downloads: 15420, rating: 4.8, author: 'SpaceDev Studios', category: 'Exploration' },
      { id: '2', name: 'Quantum Builder', downloads: 8930, rating: 4.9, author: 'Quantum Labs', category: 'Construction' },
      { id: '3', name: 'Neural Network Manager', downloads: 25670, rating: 4.7, author: 'AI Innovations', category: 'AI/ML' },
    ],
    topRuntimes: [
      { id: '1', name: 'JavaScript Engine', active: 189, total: 234, type: 'Programming Language', status: 'Running' },
      { id: '2', name: 'Python Interpreter', active: 156, total: 189, type: 'Programming Language', status: 'Running' },
      { id: '3', name: 'Node.js Runtime', active: 134, total: 167, type: 'Web Runtime', status: 'Running' },
    ],
    topTemplates: [
      { id: '1', name: 'React SPA Template', downloads: 234000, rating: 4.8, author: 'React Team', category: 'Web App' },
      { id: '2', name: 'Vue.js SPA Template', downloads: 89000, rating: 4.7, author: 'Vue Team', category: 'Web App' },
      { id: '3', name: 'Angular Enterprise', downloads: 67000, rating: 4.6, author: 'Microsoft', category: 'Web App' },
    ],
    storeItems: [
      { id: '1', name: 'Quantum Engine Core', price: 25000, sales: 1247, category: 'Hardware', rating: 4.9 },
      { id: '2', name: 'Neural Network SDK', price: 8500, sales: 892, category: 'Software', rating: 4.8 },
      { id: '3', name: 'Holographic Display Array', price: 45000, sales: 156, category: 'Display', rating: 4.7 },
    ],
    systemMetrics: {
      cpuUsage: 45.2,
      memoryUsage: 67.8,
      diskUsage: 34.5,
      networkLatency: 12.3,
      activeConnections: 892,
      totalRequests: 4567892,
    },
    userGrowth: [
      { month: 'Jan', users: 1200000, karma: 5000000 },
      { month: 'Feb', users: 1350000, karma: 6000000 },
      { month: 'Mar', users: 1500000, karma: 7500000 },
      { month: 'Apr', users: 1700000, karma: 8500000 },
      { month: 'May', users: 1900000, karma: 9500000 },
      { month: 'Jun', users: 2200000, karma: 11000000 },
      { month: 'Jul', users: 2547891, karma: 12500000 },
    ],
    categoryDistribution: [
      { name: 'Web Apps', value: 35, count: 1250 },
      { name: 'Mobile Apps', value: 25, count: 892 },
      { name: 'AI/ML', value: 20, count: 714 },
      { name: 'Games', value: 15, count: 536 },
      { name: 'Blockchain', value: 5, count: 179 },
    ],
    performanceData: [
      { name: 'Jan', users: 1200000, karma: 2100000, transactions: 45000 },
      { name: 'Feb', users: 1350000, karma: 2400000, transactions: 52000 },
      { name: 'Mar', users: 1500000, karma: 2800000, transactions: 58000 },
      { name: 'Apr', users: 1680000, karma: 3200000, transactions: 65000 },
      { name: 'May', users: 1850000, karma: 3600000, transactions: 72000 },
      { name: 'Jun', users: 2050000, karma: 4100000, transactions: 78000 },
      { name: 'Jul', users: 2250000, karma: 4600000, transactions: 85000 },
    ],
    systemStatus: {
      api: { status: 'healthy', responseTime: 45, uptime: 99.9 },
      database: { status: 'healthy', responseTime: 12, uptime: 99.8 },
      storage: { status: 'healthy', responseTime: 8, uptime: 99.9 },
      cache: { status: 'healthy', responseTime: 2, uptime: 99.9 },
      cdn: { status: 'healthy', responseTime: 15, uptime: 99.9 },
    },
    topPerformers: [
      { name: 'Quantum Calculator', type: 'OAPP', users: 45678, karma: 1250000, growth: 25.3 },
      { name: 'Cosmic Dragon NFT', type: 'NFT', users: 23456, karma: 890000, growth: 18.7 },
      { name: 'AI Assistant', type: 'Plugin', users: 78901, karma: 1560000, growth: 32.1 },
      { name: 'Space Explorer', type: 'Avatar', users: 123456, karma: 2340000, growth: 15.8 },
      { name: 'Neural Network SDK', type: 'Library', users: 34567, karma: 670000, growth: 22.4 },
    ],
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
