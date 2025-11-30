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
  // Handle nested result structure (OASIS API often returns result.result)
  let candidate = payload;
  if (payload?.result) {
    candidate = payload.result;
    // Check for double nesting (result.result)
    if (candidate?.result) {
      candidate = candidate.result;
    }
  }
  
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
    // Use the dedicated authentication route that handles self-signed certs via curl
    // This matches the working implementation in nft-mint-frontend
    try {
      const authUrl = USE_PROXY 
        ? '/api/authenticate' 
        : `${this.baseUrl}/api/avatar/authenticate`;
      
      const response = await fetch(authUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: JSON.stringify({ username, password, baseUrl: this.baseUrl }),
        mode: USE_PROXY ? 'same-origin' : 'cors',
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
        throw new Error(errorData.message || `Authentication failed (HTTP ${response.status})`);
      }

      const data = await response.json();
      
      // The authenticate route returns { token, avatarId, avatar, message }
      if (!data.token) {
        throw new Error(data.message || 'No token received from authentication service');
      }

      // Use avatar data from authentication response (already includes full profile)
      const avatar = normalizeAvatar(data.avatar) || {
        avatarId: data.avatarId || username,
        id: data.avatarId || username,
        username: username,
      };

      return {
        avatar,
        jwtToken: data.token,
        refreshToken: null,
        expiresIn: undefined,
      };
    } catch (error) {
      console.error('Avatar login failed:', error);
      throw error instanceof Error ? error : new Error('Unable to authenticate avatar');
    }
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

