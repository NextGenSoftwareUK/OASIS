#!/usr/bin/env node
// Launcher shim — execs the downloaded native binary with all args passed through.
const path   = require('path');
const os     = require('os');
const fs     = require('fs');
const { execFileSync } = require('child_process');

const bin = process.platform === 'win32'
  ? path.join(__dirname, 'oasis-mcp.exe')
  : path.join(__dirname, 'oasis-mcp');

if (!fs.existsSync(bin)) {
  console.error('[oasis-mcp] Binary not found. Try reinstalling: npm install -g @oasisomniverse/mcp-server');
  process.exit(1);
}

try {
  execFileSync(bin, process.argv.slice(2), { stdio: 'inherit' });
} catch (e) {
  process.exit(e.status ?? 1);
}
