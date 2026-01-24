#!/usr/bin/env node

// Disable SSL verification for local development (before any imports)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { oasisTools, handleOASISTool } from './tools/oasisTools.js';
import { smartContractTools, handleSmartContractTool } from './tools/smartContractTools.js';
import { config } from './config.js';
import { createLicenseValidator } from './license.js';

/**
 * Unified OASIS MCP Server
 * 
 * Provides MCP tools for interacting with:
 * - OASIS API (avatars, karma, NFTs, wallets, holons)
 * - SmartContractGenerator (generate, compile, deploy contracts)
 * - OpenSERV (AI workflows) - Coming soon
 * - STAR (dApp creation) - Coming soon
 */

async function main() {
  // Validate license if license key is provided
  const licenseKey = process.env.OASIS_MCP_LICENSE_KEY || '';
  if (licenseKey) {
    try {
      const validator = createLicenseValidator();
      const licenseResult = await validator.validate();
      
      if (!licenseResult.valid) {
        console.error(`[MCP] License Error: ${licenseResult.message}`);
        console.error('[MCP] Visit https://www.oasisweb4.com/products/mcp.html to get your license key.');
        console.error('[MCP] For testing without a license, remove OASIS_MCP_LICENSE_KEY from your environment.');
        // Don't exit - allow free tier usage
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
  // Create MCP server
  const server = new Server(
    {
      name: 'oasis-unified-mcp',
      version: '1.0.0',
    },
    {
      capabilities: {
        tools: {},
      },
    }
  );

  // List available tools
  server.setRequestHandler(ListToolsRequestSchema, async () => {
    return {
      tools: [...oasisTools, ...smartContractTools],
    };
  });

  // Handle tool calls
  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;

    console.error(`[MCP] Tool called: ${name}`, JSON.stringify(args, null, 2));

    try {
        // Route to appropriate handler
        let result;
        if (name.startsWith('oasis_') || name.startsWith('solana_') || name.startsWith('glif_') || name.startsWith('nanobanana_') || name.startsWith('ltx_')) {
          result = await handleOASISTool(name, args || {});
      } else if (name.startsWith('scgen_')) {
        result = await handleSmartContractTool(name, args || {});
      } else {
        throw new Error(`Unknown tool prefix: ${name}`);
      }

      // Check if result contains image data for preview
      const content: any[] = [];
      
      if (result && result.imageBase64 && result.mimeType) {
        // Include image preview
        content.push({
          type: 'image',
          mimeType: result.mimeType,
          data: result.imageBase64,
        });
      }
      
      // Always include text response with metadata
      // Remove imageBase64 from text response to keep it clean (it's already in image content)
      const textResult = { ...result };
      if (textResult.imageBase64) {
        delete textResult.imageBase64;
      }
      
      content.push({
        type: 'text',
        text: JSON.stringify(textResult, null, 2),
      });

      return { content };
    } catch (error: any) {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(
              {
                error: true,
                message: error.message || 'Unknown error',
                details: error.stack,
              },
              null,
              2
            ),
          },
        ],
        isError: true,
      };
    }
  });

  // Connect via stdio
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

