import { Tool } from '@modelcontextprotocol/sdk/types.js';
import axios, { AxiosInstance } from 'axios';
import { config } from '../config.js';

// STAR WebAPI client (port 5001)
class STARClient {
  private client: AxiosInstance;
  private token: string | null = null;
  private lastCredentials: { username: string; password: string } | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: config.starApiUrl,
      headers: { 'Content-Type': 'application/json' },
      timeout: 60000,
    });
  }

  setToken(token: string) {
    this.token = token;
  }

  setCredentials(username: string, password: string) {
    this.lastCredentials = { username, password };
  }

  private authHeaders(): Record<string, string> {
    return this.token ? { Authorization: `Bearer ${this.token}` } : {};
  }

  private findJwt(d: any): string | null {
    if (!d || typeof d !== 'object') return null;
    for (const [k, v] of Object.entries(d)) {
      if (typeof v === 'string' && v.length > 30 && (k.toLowerCase().includes('jwt') || k.toLowerCase() === 'token')) return v;
      const nested = this.findJwt(v);
      if (nested) return nested;
    }
    return null;
  }

  // Re-authenticate if we have stored credentials (token expired or 401)
  private async refreshToken(): Promise<boolean> {
    if (!this.lastCredentials) return false;
    try {
      const res = await this.client.post('/api/Avatar/authenticate', this.lastCredentials, {
        headers: { 'Content-Type': 'application/json' },
      });
      const jwt = this.findJwt(res.data);
      if (jwt) { this.token = jwt; return true; }
    } catch { /* ignore */ }
    return false;
  }

  private async requestWithRetry(fn: () => Promise<any>): Promise<any> {
    try {
      return await fn();
    } catch (err: any) {
      // On 401 (token expired), try to refresh and retry once
      if (err?.response?.status === 401 || err?.response?.data?.message?.includes('JWT Token Is Invalid')) {
        const refreshed = await this.refreshToken();
        if (refreshed) return await fn();
      }
      throw err;
    }
  }

  async get(path: string, params?: Record<string, any>) {
    return this.requestWithRetry(async () => {
      const res = await this.client.get(path, { params, headers: this.authHeaders() });
      return res.data;
    });
  }

  async post(path: string, body?: any) {
    return this.requestWithRetry(async () => {
      const res = await this.client.post(path, body ?? {}, { headers: this.authHeaders() });
      return res.data;
    });
  }

  async put(path: string, body?: any) {
    return this.requestWithRetry(async () => {
      const res = await this.client.put(path, body ?? {}, { headers: this.authHeaders() });
      return res.data;
    });
  }

  async del(path: string) {
    return this.requestWithRetry(async () => {
      const res = await this.client.delete(path, { headers: this.authHeaders() });
      return res.data;
    });
  }
}

const star = new STARClient();

// ─── Tool definitions ────────────────────────────────────────────────────────

export const starTools: Tool[] = [
  // ── System ──────────────────────────────────────────────────────────────────
  {
    name: 'star_get_status',
    description: 'Get the current status of the STAR system (ignited / extinguished)',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_ignite',
    description: 'Ignite (boot) the STAR system. Must be called before any other STAR operations.',
    inputSchema: {
      type: 'object',
      properties: {
        username: { type: 'string', description: 'OASIS avatar username (optional)' },
        password: { type: 'string', description: 'OASIS avatar password (optional)' },
      },
    },
  },
  {
    name: 'star_extinguish',
    description: 'Extinguish (shut down) the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_beam_in',
    description: 'Authenticate an avatar to the STAR system',
    inputSchema: {
      type: 'object',
      properties: {
        username: { type: 'string', description: 'Avatar username' },
        password: { type: 'string', description: 'Avatar password' },
      },
      required: ['username', 'password'],
    },
  },

  // ── OAPPs ────────────────────────────────────────────────────────────────────
  {
    name: 'star_list_oapps',
    description: 'List all OAPPs (Omniverse Applications) in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_oapp',
    description: 'Get details of a specific OAPP by its ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_create_oapp',
    description: 'Create a new OAPP (Omniverse Application) in the STAR system',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Name of the OAPP' },
        description: { type: 'string', description: 'Description of the OAPP' },
        version: { type: 'string', description: 'Version string, e.g. "1.0.0"' },
        oappType: {
          type: 'number',
          description: 'OAPPType enum value: 0=Default, 1=Console, 2=WebAPI, 3=Blazor, 4=MAUI, 5=OAPPTemplate, 6=GeneratedCodeOnly',
        },
      },
      required: ['name', 'description'],
    },
  },
  {
    name: 'star_update_oapp',
    description: 'Update an existing OAPP',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        name: { type: 'string', description: 'New name' },
        description: { type: 'string', description: 'New description' },
        version: { type: 'string', description: 'New version' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_delete_oapp',
    description: 'Delete an OAPP by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_clone_oapp',
    description: 'Clone an existing OAPP with a new name',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'Source OAPP UUID' },
        newName: { type: 'string', description: 'Name for the cloned OAPP' },
      },
      required: ['id', 'newName'],
    },
  },
  {
    name: 'star_publish_oapp',
    description: 'Publish an OAPP to the STARNET store',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        sourcePath: { type: 'string', description: 'Path to the OAPP source folder' },
        launchTarget: { type: 'string', description: 'Launch target (e.g. executable name)' },
        publishPath: { type: 'string', description: 'Output publish path (optional)' },
        registerOnSTARNET: { type: 'boolean', description: 'Register on STARNET store (default true)', default: true },
        generateBinary: { type: 'boolean', description: 'Generate binary package', default: false },
        uploadToCloud: { type: 'boolean', description: 'Upload to cloud storage', default: false },
      },
      required: ['id', 'sourcePath', 'launchTarget'],
    },
  },
  {
    name: 'star_unpublish_oapp',
    description: 'Unpublish an OAPP from the STARNET store',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        version: { type: 'number', description: 'Version to unpublish (0 = latest)', default: 0 },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_republish_oapp',
    description: 'Republish an OAPP to the STARNET store',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        version: { type: 'number', description: 'Version to republish (0 = latest)', default: 0 },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_activate_oapp',
    description: 'Activate an OAPP on the STARNET store (make it visible/available)',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        version: { type: 'number', description: 'Version to activate (0 = latest)', default: 0 },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_deactivate_oapp',
    description: 'Deactivate an OAPP on the STARNET store',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        version: { type: 'number', description: 'Version to deactivate (0 = latest)', default: 0 },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_search_oapps',
    description: 'Search for OAPPs by name or description',
    inputSchema: {
      type: 'object',
      properties: {
        searchTerm: { type: 'string', description: 'Term to search for' },
      },
      required: ['searchTerm'],
    },
  },
  {
    name: 'star_download_oapp',
    description: 'Download an OAPP from the STARNET store',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'OAPP UUID' },
        destinationPath: { type: 'string', description: 'Local folder to download into' },
        overwrite: { type: 'boolean', description: 'Overwrite if already exists', default: false },
      },
      required: ['id', 'destinationPath'],
    },
  },

  // ── Quests ───────────────────────────────────────────────────────────────────
  {
    name: 'star_list_quests',
    description: 'List all quests in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_quest',
    description: 'Get details of a specific quest by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'Quest UUID' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_create_quest',
    description: 'Create a new quest in the STAR system',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Quest name' },
        description: { type: 'string', description: 'Quest description' },
      },
      required: ['name', 'description'],
    },
  },

  // ── Celestial Bodies ─────────────────────────────────────────────────────────
  {
    name: 'star_list_celestial_bodies',
    description: 'List all celestial bodies (planets, moons, stars, etc.) in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_celestial_body',
    description: 'Get details of a specific celestial body by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'Celestial body UUID' },
      },
      required: ['id'],
    },
  },

  // ── Zomes ────────────────────────────────────────────────────────────────────
  {
    name: 'star_list_zomes',
    description: 'List all zomes (modular components that group holons) in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_zome',
    description: 'Get details of a specific zome by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'Zome UUID' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_create_zome',
    description: 'Create a new zome in the STAR system',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Zome name' },
        description: { type: 'string', description: 'Zome description' },
      },
      required: ['name', 'description'],
    },
  },

  // ── Holons ───────────────────────────────────────────────────────────────────
  {
    name: 'star_list_holons',
    description: 'List all holons (data objects / building blocks) in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_holon',
    description: 'Get a specific holon by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'Holon UUID' },
      },
      required: ['id'],
    },
  },
  {
    name: 'star_create_holon',
    description: 'Create a new holon (data object) in the STAR system',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Holon name' },
        description: { type: 'string', description: 'Holon description' },
        customData: { type: 'object', description: 'Custom metadata key-value pairs' },
      },
      required: ['name'],
    },
  },

  // ── NFTs (via STAR) ──────────────────────────────────────────────────────────
  {
    name: 'star_list_nfts',
    description: 'List all NFTs managed by the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_get_nft',
    description: 'Get details of a specific NFT by ID',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'NFT UUID or mint address' },
      },
      required: ['id'],
    },
  },

  // ── Plugins ──────────────────────────────────────────────────────────────────
  {
    name: 'star_list_plugins',
    description: 'List all plugins in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },

  // ── Missions & Chapters ──────────────────────────────────────────────────────
  {
    name: 'star_list_missions',
    description: 'List all missions in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'star_list_chapters',
    description: 'List all chapters in the STAR system',
    inputSchema: { type: 'object', properties: {} },
  },

  // ── Health ───────────────────────────────────────────────────────────────────
  {
    name: 'star_health_check',
    description: 'Check the health of the STAR WebAPI server',
    inputSchema: { type: 'object', properties: {} },
  },
];

// ─── Tool handlers ───────────────────────────────────────────────────────────

export async function handleSTARTool(name: string, args: Record<string, any>): Promise<any> {
  try {
    switch (name) {
      // System
      case 'star_get_status':
        return await star.get('/api/STAR/status');

      case 'star_ignite':
        return await star.post('/api/STAR/ignite', {
          userName: args.username,
          password: args.password,
        });

      case 'star_extinguish':
        return await star.post('/api/STAR/extinguish');

      case 'star_beam_in': {
        const beamResult = await star.post('/api/STAR/beam-in', {
          username: args.username,
          password: args.password,
        });
        // beam-in authenticates to STAR but doesn't return a JWT.
        // Also call Avatar/authenticate to get the JWT needed for all subsequent API calls.
        const findJwt = (d: any): string | null => {
          if (!d || typeof d !== 'object') return null;
          for (const [k, v] of Object.entries(d)) {
            if (typeof v === 'string' && v.length > 30 && (k.toLowerCase().includes('jwt') || k.toLowerCase().includes('token'))) return v;
            const nested = findJwt(v);
            if (nested) return nested;
          }
          return null;
        };
        try {
          const avatarAuth = await star.post('/api/Avatar/authenticate', {
            username: args.username,
            password: args.password,
          });
          const jwt = findJwt(avatarAuth);
          if (jwt) {
            star.setToken(jwt);
            star.setCredentials(args.username, args.password);
            (beamResult as any)._jwtSet = true;
          }
        } catch {
          // avatar auth failed — continue without JWT
        }
        return beamResult;
      }

      // OAPPs
      case 'star_list_oapps':
        return await star.get('/api/OAPPs');

      case 'star_get_oapp':
        return await star.get(`/api/OAPPs/${args.id}`);

      case 'star_create_oapp':
        return await star.post('/api/OAPPs/create', {
          name: args.name,
          description: args.description,
          holonSubType: args.oappType ?? 0,
          sourceFolderPath: args.sourceFolderPath ?? '',
        });

      case 'star_update_oapp':
        return await star.put(`/api/OAPPs/${args.id}`, {
          id: args.id,
          name: args.name,
          description: args.description,
          version: args.version,
        });

      case 'star_delete_oapp':
        return await star.del(`/api/OAPPs/${args.id}`);

      case 'star_clone_oapp':
        return await star.post(`/api/OAPPs/${args.id}/clone`, {
          newName: args.newName,
        });

      case 'star_publish_oapp':
        return await star.post(`/api/OAPPs/${args.id}/publish`, {
          sourcePath: args.sourcePath,
          launchTarget: args.launchTarget,
          publishPath: args.publishPath ?? '',
          edit: false,
          registerOnSTARNET: args.registerOnSTARNET ?? true,
          generateBinary: args.generateBinary ?? false,
          uploadToCloud: args.uploadToCloud ?? false,
        });

      case 'star_unpublish_oapp':
        return await star.post(`/api/OAPPs/${args.id}/unpublish?version=${args.version ?? 0}`);

      case 'star_republish_oapp':
        return await star.post(`/api/OAPPs/${args.id}/republish?version=${args.version ?? 0}`);

      case 'star_activate_oapp':
        return await star.post(`/api/OAPPs/${args.id}/activate?version=${args.version ?? 0}`);

      case 'star_deactivate_oapp':
        return await star.post(`/api/OAPPs/${args.id}/deactivate?version=${args.version ?? 0}`);

      case 'star_search_oapps':
        return await star.post('/api/OAPPs/search', { searchTerm: args.searchTerm });

      case 'star_download_oapp':
        return await star.post(`/api/OAPPs/${args.id}/download`, {
          destinationPath: args.destinationPath,
          overwrite: args.overwrite ?? false,
        });

      // Quests
      case 'star_list_quests':
        return await star.get('/api/Quests');

      case 'star_get_quest':
        return await star.get(`/api/Quests/${args.id}`);

      case 'star_create_quest':
        return await star.post('/api/Quests', {
          name: args.name,
          description: args.description,
        });

      // Celestial Bodies
      case 'star_list_celestial_bodies':
        return await star.get('/api/CelestialBodies');

      case 'star_get_celestial_body':
        return await star.get(`/api/CelestialBodies/${args.id}`);

      // Zomes
      case 'star_list_zomes':
        return await star.get('/api/Zomes');

      case 'star_get_zome':
        return await star.get(`/api/Zomes/${args.id}`);

      case 'star_create_zome':
        return await star.post('/api/Zomes', {
          name: args.name,
          description: args.description,
        });

      // Holons
      case 'star_list_holons':
        return await star.get('/api/Holons');

      case 'star_get_holon':
        return await star.get(`/api/Holons/${args.id}`);

      case 'star_create_holon':
        return await star.post('/api/Holons', {
          name: args.name,
          description: args.description,
          metaData: args.customData,
        });

      // NFTs
      case 'star_list_nfts':
        return await star.get('/api/NFTs');

      case 'star_get_nft':
        return await star.get(`/api/NFTs/${args.id}`);

      // Plugins
      case 'star_list_plugins':
        return await star.get('/api/Plugins');

      // Missions & Chapters
      case 'star_list_missions':
        return await star.get('/api/Missions');

      case 'star_list_chapters':
        return await star.get('/api/Chapters');

      // Health
      case 'star_health_check':
        return await star.get('/api/Health');

      default:
        throw new Error(`Unknown STAR tool: ${name}`);
    }
  } catch (err: any) {
    const status = err?.response?.status;
    const data = err?.response?.data;
    return {
      error: true,
      message: err.message,
      status,
      details: data,
      hint: status === undefined
        ? 'Could not connect to STAR WebAPI. Make sure it is running on ' + config.starApiUrl
        : undefined,
    };
  }
}
