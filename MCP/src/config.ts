import dotenv from 'dotenv';
import path from 'path';
import { fileURLToPath } from 'url';

// Get the directory of the current module
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Load .env file from the MCP directory (where this file is located)
dotenv.config({ path: path.resolve(__dirname, '../../.env') });

export const config = {
  oasisApiUrl: process.env.OASIS_API_URL || 'https://127.0.0.1:5004',
  openservApiUrl: process.env.OPENSERV_API_URL || 'http://127.0.0.1:8080',
  starApiUrl: process.env.STAR_API_URL || 'http://127.0.0.1:5001',
  smartContractApiUrl: process.env.SMART_CONTRACT_API_URL || 'http://127.0.0.1:5000',
  glifApiUrl: process.env.GLIF_API_URL || 'https://simple-api.glif.app',
  glifApiToken: process.env.GLIF_API_TOKEN || '',
  ltxApiUrl: process.env.LTX_API_URL || 'https://api.ltx.video/v1',
  ltxApiToken: process.env.LTX_API_TOKEN || '',
  bananaApiUrl: process.env.BANANA_API_URL || 'https://api.banana.dev',
  bananaApiKey: process.env.BANANA_API_KEY || '',
  nanoBananaApiUrl: process.env.NANO_BANANA_API_URL || 'https://api.nanobananaapi.ai/api/v1/nanobanana',
  nanoBananaApiKey: process.env.NANO_BANANA_API_KEY || '',
  oasisApiKey: process.env.OASIS_API_KEY || '',
  openservApiKey: process.env.OPENSERV_API_KEY || '',
  mode: process.env.MCP_MODE || 'stdio', // 'stdio' | 'http'
  port: parseInt(process.env.PORT || '3000', 10),
} as const;

