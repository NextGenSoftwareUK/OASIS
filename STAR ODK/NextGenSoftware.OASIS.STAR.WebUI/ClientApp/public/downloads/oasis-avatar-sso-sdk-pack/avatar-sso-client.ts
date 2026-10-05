export interface OasisAvatarSSOOptions {
  clientId: string;
  redirectUri: string;
  provider?: string;
  scope?: string[];
}

export interface AvatarSession {
  token: string;
  avatar: {
    id: string;
    username: string;
    karma: number;
    providers: string[];
  };
  expiresAt: string;
}

export class OasisAvatarSSO {
  constructor(private readonly options: OasisAvatarSSOOptions) {}

  async login(): Promise<void> {
    window.location.href = `${process.env.REACT_APP_OASIS_SSO_URL}/authorize?client_id=${this.options.clientId}&redirect_uri=${encodeURIComponent(this.options.redirectUri)}&provider=${this.options.provider || 'Auto'}`;
  }

  async getCurrentSession(): Promise<AvatarSession> {
    const response = await fetch(`${process.env.REACT_APP_OASIS_API_URL}/avatar/session`, {
      credentials: 'include',
    });

    if (!response.ok) {
      throw new Error('Failed to fetch avatar session');
    }

    return response.json();
  }

  async logout(): Promise<void> {
    await fetch(`${process.env.REACT_APP_OASIS_API_URL}/avatar/logout`, {
      method: 'POST',
      credentials: 'include',
    });
  }

  render(config: { containerId: string; theme?: string; providers?: string[] }): void {
    const container = document.getElementById(config.containerId);
    if (!container) {
      throw new Error(`Container with id "${config.containerId}" not found`);
    }

    container.innerHTML = `
      <div class="oasis-avatar-widget ${config.theme || 'starnet-dark'}">
        <h3>Beam into the OASIS</h3>
        <button id="oasis-sso-login">Login with Avatar</button>
      </div>
    `;

    container.querySelector('#oasis-sso-login')?.addEventListener('click', () => this.login());
  }
}


