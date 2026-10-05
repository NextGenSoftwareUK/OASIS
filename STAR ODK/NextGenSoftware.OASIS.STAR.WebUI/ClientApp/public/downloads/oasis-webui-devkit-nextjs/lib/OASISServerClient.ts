/**
 * OASIS Server Client for Next.js Server Components and API Routes
 * Use this in Server Components, Server Actions, and API Routes
 */
export class OASISServerClient {
  private apiUrl: string;

  constructor(apiUrl?: string) {
    this.apiUrl = apiUrl || process.env.OASIS_API_URL || 'https://api.oasis.network';
  }

  // Avatar methods
  async getAvatarDetail(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}`, {
      cache: 'no-store' // or 'force-cache' for static data
    });
    return response.json();
  }

  async updateAvatar(avatarId: string, data: any) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return response.json();
  }

  // Karma methods
  async getKarma(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}/karma`, {
      cache: 'no-store'
    });
    return response.json();
  }

  async addKarma(avatarId: string, amount: number, reason: string) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}/karma`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ amount, reason })
    });
    return response.json();
  }

  async getKarmaLeaderboard(timeRange: string, limit: number) {
    const response = await fetch(
      `${this.apiUrl}/karma/leaderboard?range=${timeRange}&limit=${limit}`,
      { cache: 'no-store' }
    );
    return response.json();
  }

  // NFT methods
  async getNFTs(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/nfts?avatarId=${avatarId}`, {
      cache: 'no-store'
    });
    return response.json();
  }

  async mintNFT(avatarId: string, nftData: any) {
    const response = await fetch(`${this.apiUrl}/nfts/mint`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ avatarId, ...nftData })
    });
    return response.json();
  }

  // Messaging methods
  async getChatMessages(chatId: string) {
    const response = await fetch(`${this.apiUrl}/chat/${chatId}/messages`, {
      cache: 'no-store'
    });
    return response.json();
  }

  async sendMessage(chatId: string, avatarId: string, content: string) {
    const response = await fetch(`${this.apiUrl}/chat/messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ chatId, avatarId, content })
    });
    return response.json();
  }

  // Data management methods
  async getData(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/data/${avatarId}`, {
      cache: 'no-store'
    });
    return response.json();
  }

  async saveData(avatarId: string, key: string, value: any) {
    const response = await fetch(`${this.apiUrl}/data/${avatarId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value })
    });
    return response.json();
  }

  // Social methods
  async getSocialFeed(feedType: string, avatarId?: string) {
    const url = avatarId 
      ? `${this.apiUrl}/social/feed?type=${feedType}&avatarId=${avatarId}`
      : `${this.apiUrl}/social/feed?type=${feedType}`;
    
    const response = await fetch(url, { cache: 'no-store' });
    return response.json();
  }

  // Friends methods
  async getFriends(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}/friends`, {
      cache: 'no-store'
    });
    return response.json();
  }

  // Achievements methods
  async getAchievements(avatarId: string) {
    const response = await fetch(`${this.apiUrl}/avatars/${avatarId}/achievements`, {
      cache: 'no-store'
    });
    return response.json();
  }
}

