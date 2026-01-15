#!/usr/bin/env node

// Disable SSL verification for local development (before any imports)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { oasisTools, handleOASISTool } from './tools/oasisTools.js';
import { smartContractTools, handleSmartContractTool } from './tools/smartContractTools.js';
import { config } from './config.js';

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
      if (name.startsWith('oasis_') || name.startsWith('solana_')) {
        result = await handleOASISTool(name, args || {});
      } else if (name.startsWith('scgen_')) {
        result = await handleSmartContractTool(name, args || {});
      } else {
        throw new Error(`Unknown tool prefix: ${name}`);
      }

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(result, null, 2),
          },
        ],
      };
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

