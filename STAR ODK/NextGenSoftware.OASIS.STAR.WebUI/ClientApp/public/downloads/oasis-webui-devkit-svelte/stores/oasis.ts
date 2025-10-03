import { writable, derived, get } from 'svelte/store';
import { OASISClient } from '@oasis/api-client';

// Initialize OASIS client
const client = new OASISClient({
  apiEndpoint: 'https://api.oasis.network'
});

// Core stores
export const currentAvatar = writable<any>(null);
export const isAuthenticated = derived(currentAvatar, $avatar => !!$avatar);
export const karma = writable<any>(null);
export const nfts = writable<any[]>([]);
export const friends = writable<any[]>([]);
export const achievements = writable<any[]>([]);

// Authentication
export async function login(provider: string) {
  try {
    const avatar = await client.authenticateWithProvider(provider);
    currentAvatar.set(avatar);
    return avatar;
  } catch (error) {
    console.error('Login failed:', error);
    throw error;
  }
}

export function logout() {
  currentAvatar.set(null);
  karma.set(null);
  nfts.set([]);
  friends.set([]);
  achievements.set([]);
}

// Karma functions
export async function loadKarma(avatarId: string) {
  try {
    const karmaData = await client.getAvatarKarma(avatarId);
    karma.set(karmaData);
    return karmaData;
  } catch (error) {
    console.error('Failed to load karma:', error);
    throw error;
  }
}

export async function addKarma(avatarId: string, amount: number, reason: string) {
  try {
    await client.addKarma(avatarId, amount, reason);
    await loadKarma(avatarId);
  } catch (error) {
    console.error('Failed to add karma:', error);
    throw error;
  }
}

// NFT functions
export async function loadNFTs(avatarId: string) {
  try {
    const nftData = await client.getNFTs(avatarId);
    nfts.set(nftData);
    return nftData;
  } catch (error) {
    console.error('Failed to load NFTs:', error);
    throw error;
  }
}

export async function mintNFT(avatarId: string, nftData: any) {
  try {
    const result = await client.mintNFT(avatarId, nftData);
    await loadNFTs(avatarId);
    return result;
  } catch (error) {
    console.error('Failed to mint NFT:', error);
    throw error;
  }
}

// Friends functions
export async function loadFriends(avatarId: string) {
  try {
    const friendsData = await client.getFriends(avatarId);
    friends.set(friendsData);
    return friendsData;
  } catch (error) {
    console.error('Failed to load friends:', error);
    throw error;
  }
}

// Achievements functions
export async function loadAchievements(avatarId: string) {
  try {
    const achievementsData = await client.getAchievements(avatarId);
    achievements.set(achievementsData);
    return achievementsData;
  } catch (error) {
    console.error('Failed to load achievements:', error);
    throw error;
  }
}

// Export client for direct access
export { client };

