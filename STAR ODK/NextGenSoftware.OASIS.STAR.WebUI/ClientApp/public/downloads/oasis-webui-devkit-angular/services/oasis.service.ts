import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OASISService {
  private apiUrl = 'https://api.oasis.network';

  constructor(private http: HttpClient) {}

  // Authentication
  authenticateWithProvider(provider: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/auth/provider`, { provider }).toPromise();
  }

  // Avatar
  getAvatarDetail(avatarId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/avatars/${avatarId}`);
  }

  updateAvatar(avatarId: string, data: any): Promise<any> {
    return this.http.put(`${this.apiUrl}/avatars/${avatarId}`, data).toPromise();
  }

  // Karma
  getAvatarKarma(avatarId: string): Promise<any> {
    return this.http.get(`${this.apiUrl}/avatars/${avatarId}/karma`).toPromise();
  }

  getKarmaHistory(avatarId: string): Promise<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/avatars/${avatarId}/karma/history`).toPromise();
  }

  getKarmaLeaderboard(timeRange: string, limit: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/karma/leaderboard?range=${timeRange}&limit=${limit}`);
  }

  // NFTs
  getNFTs(avatarId?: string, collections?: string[]): Observable<any[]> {
    const params = avatarId ? `?avatarId=${avatarId}` : '';
    return this.http.get<any[]>(`${this.apiUrl}/nfts${params}`);
  }

  mintNFT(avatarId: string, nftData: any): Promise<any> {
    return this.http.post(`${this.apiUrl}/nfts/mint`, { avatarId, ...nftData }).toPromise();
  }

  transferNFT(nftId: string, toAddress: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/nfts/${nftId}/transfer`, { toAddress }).toPromise();
  }

  // Geo-NFTs
  getGeoNFTs(avatarId?: string): Promise<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/geonft`).toPromise();
  }

  mintGeoNFT(avatarId: string, data: any): Promise<any> {
    return this.http.post(`${this.apiUrl}/geonft/mint`, { avatarId, ...data }).toPromise();
  }

  // Messaging
  getChatMessages(chatId: string): Promise<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/chat/${chatId}/messages`).toPromise();
  }

  sendMessage(message: any): Promise<any> {
    return this.http.post(`${this.apiUrl}/chat/messages`, message).toPromise();
  }

  subscribeToChat(chatId: string): Observable<any> {
    // WebSocket or Server-Sent Events implementation
    return of({});
  }

  // Data Management
  getAvatarData(avatarId: string): Promise<any> {
    return this.http.get(`${this.apiUrl}/data/${avatarId}`).toPromise();
  }

  saveData(avatarId: string, key: string, value: any): Promise<any> {
    return this.http.post(`${this.apiUrl}/data/${avatarId}`, { key, value }).toPromise();
  }

  deleteData(avatarId: string, key: string): Promise<any> {
    return this.http.delete(`${this.apiUrl}/data/${avatarId}/${key}`).toPromise();
  }

  // Providers
  getAvailableProviders(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/providers`);
  }

  getCurrentProvider(): Observable<any> {
    return this.http.get(`${this.apiUrl}/providers/current`);
  }

  switchProvider(providerId: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/providers/switch`, { providerId }).toPromise();
  }

  // Settings
  updateSettings(avatarId: string, settings: any): Promise<any> {
    return this.http.put(`${this.apiUrl}/avatars/${avatarId}/settings`, settings).toPromise();
  }

  // Notifications
  subscribeToNotifications(avatarId: string): Observable<any> {
    // WebSocket or Server-Sent Events implementation
    return of({});
  }

  // Social
  getSocialFeed(feedType: string, avatarId?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/social/feed?type=${feedType}`);
  }

  createPost(avatarId: string, content: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/social/posts`, { avatarId, content }).toPromise();
  }

  likePost(postId: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/social/posts/${postId}/like`, {}).toPromise();
  }

  // Friends
  getFriends(avatarId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/avatars/${avatarId}/friends`);
  }

  removeFriend(avatarId: string, friendId: string): Promise<any> {
    return this.http.delete(`${this.apiUrl}/avatars/${avatarId}/friends/${friendId}`).toPromise();
  }

  // Groups
  getGroups(avatarId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/avatars/${avatarId}/groups`);
  }

  createGroup(avatarId: string, groupData: any): Promise<any> {
    return this.http.post(`${this.apiUrl}/groups`, { ...groupData, ownerId: avatarId }).toPromise();
  }

  leaveGroup(avatarId: string, groupId: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/groups/${groupId}/leave`, { avatarId }).toPromise();
  }

  // Achievements
  getAchievements(avatarId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/avatars/${avatarId}/achievements`);
  }
}

