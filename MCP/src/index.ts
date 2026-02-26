#!/usr/bin/env node

// Disable SSL verification for local development (before any imports)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { createOasisMcpServer } from './serverFactory.js';
import { config } from './config.js';
import { createLicenseValidator } from './license.js';

async function main() {
  const licenseKey = process.env.OASIS_MCP_LICENSE_KEY || '';
  if (licenseKey) {
    try {
      const validator = createLicenseValidator();
      const licenseResult = await validator.validate();
      if (!licenseResult.valid) {
        console.error(`[MCP] License Error: ${licenseResult.message}`);
        console.error('[MCP] Visit https://www.oasisweb4.com/products/mcp.html to get your license key.');
      } else {
        console.error(`[MCP] License validated: ${licenseResult.tier || 'Unknown tier'}`);
      }
    } catch (error: any) {
      console.error(`[MCP] License validation failed: ${error.message}`);
      console.error('[MCP] Continuing in free mode...');
    }
  } else {
    console.error('[MCP] Running in free mode. Get a license at https://www.oasisweb4.com/products/mcp.html');
  }

  const server = createOasisMcpServer();
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error('[MCP] OASIS Unified MCP Server started');
  console.error(`[MCP] OASIS API URL: ${config.oasisApiUrl}`);
  console.error(`[MCP] Smart Contract API URL: ${config.smartContractApiUrl}`);
  console.error(`[MCP] Mode: ${config.mode}`);
}

main().catch((error) => {
  console.error('[MCP] Fatal error:', error);
  process.exit(1);
});
