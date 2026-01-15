import dotenv from 'dotenv';

dotenv.config();

export const config = {
  oasisApiUrl: process.env.OASIS_API_URL || 'https://127.0.0.1:5004',
  openservApiUrl: process.env.OPENSERV_API_URL || 'http://127.0.0.1:8080',
  starApiUrl: process.env.STAR_API_URL || 'http://127.0.0.1:5001',
  smartContractApiUrl: process.env.SMART_CONTRACT_API_URL || 'http://127.0.0.1:5000',
  oasisApiKey: process.env.OASIS_API_KEY || '',
  openservApiKey: process.env.OPENSERV_API_KEY || '',
  mode: process.env.MCP_MODE || 'stdio', // 'stdio' | 'http'
  port: parseInt(process.env.PORT || '3000', 10),
} as const;

