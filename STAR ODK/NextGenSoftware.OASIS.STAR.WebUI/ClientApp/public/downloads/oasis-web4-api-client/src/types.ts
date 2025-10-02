/**
 * Type definitions for OASIS Web4 API Client
 */

export interface OASISConfig {
  apiUrl: string;
  timeout?: number;
  debug?: boolean;
  autoRetry?: boolean;
  maxRetries?: number;
}

export interface OASISResult<T> {
  isError: boolean;
  message: string;
  result: T | null;
  isSaved?: boolean;
}

export interface Avatar {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  image?: string;
  bio?: string;
  karma?: number;
  level?: number;
  createdDate?: string;
  lastLoginDate?: string;
  providerKey?: { [key: string]: string };
}

export interface AuthResponse {
  avatar: Avatar;
  token: string;
  refreshToken?: string;
  expiresAt?: string;
}

export interface CreateAvatarRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  acceptTerms: boolean;
}

export interface UpdateAvatarRequest {
  username?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  image?: string;
}

export interface Karma {
  total: number;
  rank?: number;
  level?: number;
  nextLevelAt?: number;
  history?: KarmaEntry[];
}

export interface KarmaEntry {
  id: string;
  amount: number;
  reason: string;
  timestamp: string;
  karmaType: string;
}

export interface AddKarmaRequest {
  amount: number;
  reason: string;
  karmaType?: string;
  karmaSourceType?: string;
}

export interface NFT {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  collection?: string;
  price?: number;
  owner: string;
  metadata?: { [key: string]: any };
  blockchain?: string;
  tokenId?: string;
  createdDate?: string;
}

export interface MintNFTRequest {
  name: string;
  description: string;
  imageUrl: string;
  collection?: string;
  price?: number;
  metadata?: { [key: string]: any };
  blockchain?: string;
}

export interface DataObject {
  [key: string]: any;
}

export interface Message {
  id: string;
  chatId: string;
  avatarId: string;
  avatarName: string;
  content: string;
  timestamp: string;
  read?: boolean;
}

export interface Provider {
  name: string;
  description: string;
  icon?: string;
  isActive: boolean;
  isAvailable: boolean;
}
