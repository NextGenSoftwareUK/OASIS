/**
 * Shared OASIS MCP server factory.
 * Used by both stdio (index.ts) and HTTP (server-http.ts) entrypoints.
 */
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { oasisTools, handleOASISTool } from './tools/oasisTools.js';
import { smartContractTools, handleSmartContractTool } from './tools/smartContractTools.js';
import { starTools, handleSTARTool } from './tools/starTools.js';

export function createOasisMcpServer(): Server {
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

  server.setRequestHandler(ListToolsRequestSchema, async () => {
    return {
      tools: [...oasisTools, ...smartContractTools, ...starTools],
    };
  });

  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;
    console.error(`[MCP] Tool called: ${name}`, JSON.stringify(args, null, 2));

    try {
      let result: any;
      if (
        name.startsWith('oasis_') ||
        name.startsWith('solana_') ||
        name.startsWith('glif_') ||
        name.startsWith('nanobanana_') ||
        name.startsWith('ltx_')
      ) {
        result = await handleOASISTool(name, args || {});
      } else if (name.startsWith('scgen_')) {
        result = await handleSmartContractTool(name, args || {});
      } else if (name.startsWith('star_')) {
        result = await handleSTARTool(name, args || {});
      } else {
        throw new Error(`Unknown tool prefix: ${name}`);
      }

      const content: any[] = [];
      if (result && result.imageBase64 && result.mimeType) {
        content.push({
          type: 'image',
          mimeType: result.mimeType,
          data: result.imageBase64,
        });
      }
      const textResult = { ...result };
      if (textResult.imageBase64) delete textResult.imageBase64;
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
              { error: true, message: error.message || 'Unknown error', details: error.stack },
              null,
              2
            ),
          },
        ],
        isError: true,
      };
    }
  });

  return server;
}
