import type { AvatarAuthResponse, AvatarProfile, AvatarRegistrationRequest, OASISResult } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';

// Use proxy in development to avoid CORS issues
const USE_PROXY = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
const PROXY_BASE_URL = '/api/proxy';

type HeadersLike = HeadersInit | undefined;

const buildHeaders = (headers?: HeadersLike) => {
  if (!headers) return {};
  if (headers instanceof Headers) {
    const result: Record<string, string> = {};
    headers.forEach((value, key) => {
      result[key] = value;
    });
    return result;
  }

  if (Array.isArray(headers)) {
    return headers.reduce<Record<string, string>>((acc, [key, value]) => {
      acc[key] = value as string;
      return acc;
    }, {});
  }

  return headers;
};

const normalizeAvatar = (avatar?: AvatarProfile | null): AvatarProfile | null => {
  if (!avatar) return null;
  return {
    ...avatar,
    id: avatar.id || avatar.avatarId,
  };
};

const normalizeAuthResponse = (payload: any): AvatarAuthResponse => {
  const candidate = payload?.result ?? payload;
  const avatar = normalizeAvatar(candidate?.avatar || candidate?.user || payload?.avatar);
  const jwtToken = candidate?.jwtToken || candidate?.token || payload?.jwtToken;
  const refreshToken = candidate?.refreshToken || candidate?.refresh || payload?.refreshToken;
  const expiresIn = candidate?.expiresIn ?? payload?.expiresIn;

  if (!avatar || !jwtToken) {
    throw new Error('Invalid authentication response from OASIS Avatar API');
  }

  return {
    avatar,
    jwtToken,
    refreshToken,
    expiresIn,
  };
};

class OASISAvatarAPI {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    // Use proxy in development to avoid CORS
    let url: string;
    if (USE_PROXY && !path.startsWith('http')) {
      // Use proxy route
      url = `${PROXY_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
    } else {
      url = path.startsWith('http') ? path : `${this.baseUrl}${path.startsWith('/') ? path : `/${path}`}`;
    }
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...buildHeaders(options.headers),
    };

    const response = await fetch(url, {
      ...options,
      headers,
      mode: USE_PROXY && !path.startsWith('http') ? 'same-origin' : 'cors',
    });

    if (!response.ok) {
      let message = `HTTP ${response.status}`;
      try {
        const text = await response.text();
        message = text || message;
      } catch {
        // ignore
      }
      throw new Error(message);
    }

    return response.json();
  }

  async login(username: string, password: string): Promise<AvatarAuthResponse> {
    const payload: Record<string, string> = {
      username,
      password,
    };

    if (username.includes('@')) {
      payload.email = username;
    }

    const endpoints = ['/api/avatar/authenticate', '/api/auth/login'];

    for (const endpoint of endpoints) {
      try {
        const data = await this.request<OASISResult<unknown> | any>(endpoint, {
          method: 'POST',
          body: JSON.stringify(payload),
        });

        if ('isError' in data && data.isError) {
          throw new Error(data.message || 'Authentication failed');
        }

        return normalizeAuthResponse(data);
      } catch (error) {
        if (endpoint === endpoints[endpoints.length - 1]) {
          throw error;
        }
      }
    }

    throw new Error('Unable to authenticate avatar');
  }

  async register(request: AvatarRegistrationRequest): Promise<AvatarAuthResponse> {
    const response = await this.request<OASISResult<unknown> | any>('/api/avatar/register', {
      method: 'POST',
      body: JSON.stringify(request),
    });

    if ('isError' in response && response.isError) {
      throw new Error(response.message || 'Unable to create avatar');
    }

    return normalizeAuthResponse(response);
  }

  async getAvatarById(avatarId: string): Promise<AvatarProfile> {
    const response = await this.request<OASISResult<AvatarProfile> | any>(`/api/avatar/${avatarId}`);
    if ('isError' in response && response.isError) {
      throw new Error(response.message || 'Unable to fetch avatar');
    }

    const avatar = response?.result || response;
    const normalized = normalizeAvatar(avatar);
    if (!normalized) {
      throw new Error('Avatar not found');
    }
    return normalized;
  }

  async getAvatarByUsername(username: string): Promise<AvatarProfile> {
    const response = await this.request<OASISResult<AvatarProfile> | any>(`/api/avatar/username/${username}`);
    if ('isError' in response && response.isError) {
      throw new Error(response.message || 'Unable to fetch avatar');
    }

    const avatar = response?.result || response;
    const normalized = normalizeAvatar(avatar);
    if (!normalized) {
      throw new Error('Avatar not found');
    }
    return normalized;
  }
}

export const avatarAPI = new OASISAvatarAPI();

