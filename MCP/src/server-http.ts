#!/usr/bin/env node

/**
 * OASIS MCP HTTP server (Streamable HTTP transport).
 * Run when MCP_MODE=http. Used for hosted deployment (e.g. AWS).
 */
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

import type { Request, Response } from 'express';
import { createMcpExpressApp } from '@modelcontextprotocol/sdk/server/express.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { createOasisMcpServer } from './serverFactory.js';
import { config } from './config.js';

const PORT = config.port;
const MCP_PATH = '/mcp';

// Allow all hosts when bound to 0.0.0.0 (e.g. in Docker/ECS)
const app = createMcpExpressApp({ host: '0.0.0.0' });

// Health check for ALB/ECS
app.get('/health', (_req: Request, res: Response) => {
  res.status(200).json({ status: 'ok', service: 'oasis-mcp' });
});

// Streamable HTTP MCP endpoint (stateless: new server + transport per request)
app.post(MCP_PATH, async (req: Request, res: Response) => {
  try {
    const server = createOasisMcpServer();
    const transport = new StreamableHTTPServerTransport({
      sessionIdGenerator: undefined, // stateless
    });
    await server.connect(transport);
    await transport.handleRequest(req, res, req.body as any);
    res.on('close', () => {
      transport.close().catch(() => {});
      server.close().catch(() => {});
    });
  } catch (error: any) {
    console.error('[MCP] HTTP request error:', error);
    if (!res.headersSent) {
      res.status(500).json({
        jsonrpc: '2.0',
        error: { code: -32603, message: 'Internal server error' },
        id: null,
      });
    }
  }
});

// GET /mcp often used for SSE; support with same handler pattern
app.get(MCP_PATH, async (req: Request, res: Response) => {
  try {
    const server = createOasisMcpServer();
    const transport = new StreamableHTTPServerTransport({
      sessionIdGenerator: undefined,
    });
    await server.connect(transport);
    await transport.handleRequest(req, res, undefined);
    res.on('close', () => {
      transport.close().catch(() => {});
      server.close().catch(() => {});
    });
  } catch (error: any) {
    console.error('[MCP] HTTP GET request error:', error);
    if (!res.headersSent) {
      res.status(500).json({
        jsonrpc: '2.0',
        error: { code: -32603, message: 'Internal server error' },
        id: null,
      });
    }
  }
});

app.listen(PORT, '0.0.0.0', () => {
  console.error(`[MCP] OASIS MCP HTTP server listening on port ${PORT}`);
  console.error(`[MCP] Streamable HTTP endpoint: http://0.0.0.0:${PORT}${MCP_PATH}`);
  console.error(`[MCP] OASIS API URL: ${config.oasisApiUrl}`);
});
