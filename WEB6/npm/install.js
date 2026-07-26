#!/usr/bin/env node
// Downloads the correct pre-built oasis-mcp binary for this platform on npm install.
const https = require('https');
const fs    = require('fs');
const path  = require('path');
const os    = require('os');

const VERSION  = require('./package.json').version;
const BASE_URL = `https://github.com/NextGenSoftwareUK/OASIS/releases/download/mcp-v${VERSION}`;
const BIN_DIR  = path.join(__dirname, 'bin');

const PLATFORM_MAP = {
  'win32-x64':   { file: 'oasis-mcp-win-x64.exe',      dest: 'oasis-mcp.exe' },
  'linux-x64':   { file: 'oasis-mcp-linux-x64',         dest: 'oasis-mcp'     },
  'linux-arm64': { file: 'oasis-mcp-linux-arm64',        dest: 'oasis-mcp'     },
  'darwin-x64':  { file: 'oasis-mcp-osx-x64',           dest: 'oasis-mcp'     },
  'darwin-arm64':{ file: 'oasis-mcp-osx-arm64',          dest: 'oasis-mcp'     },
};

const key = `${process.platform}-${os.arch()}`;
const entry = PLATFORM_MAP[key];

if (!entry) {
  console.error(`[oasis-mcp] Unsupported platform: ${key}`);
  console.error(`Supported: ${Object.keys(PLATFORM_MAP).join(', ')}`);
  console.error(`You can build from source: https://github.com/NextGenSoftwareUK/OASIS/tree/master/WEB6/NextGenSoftware.OASIS.MCP.Server`);
  process.exit(1);
}

const url  = `${BASE_URL}/${entry.file}`;
const dest = path.join(BIN_DIR, entry.dest);

fs.mkdirSync(BIN_DIR, { recursive: true });

console.log(`[oasis-mcp] Downloading binary for ${key}...`);
console.log(`[oasis-mcp] ${url}`);

function download(url, dest, redirects = 0) {
  if (redirects > 5) { console.error('[oasis-mcp] Too many redirects'); process.exit(1); }

  https.get(url, (res) => {
    if (res.statusCode === 301 || res.statusCode === 302) {
      return download(res.headers.location, dest, redirects + 1);
    }
    if (res.statusCode !== 200) {
      console.error(`[oasis-mcp] Download failed: HTTP ${res.statusCode}`);
      console.error(`If this release doesn't have binaries yet, build from source with: dotnet publish -c Release -r ${key.replace('darwin','osx')} -p:PublishSingleFile=true`);
      process.exit(1);
    }

    const file = fs.createWriteStream(dest);
    res.pipe(file);
    file.on('finish', () => {
      file.close();
      if (process.platform !== 'win32') fs.chmodSync(dest, 0o755);
      console.log(`[oasis-mcp] Installed to ${dest}`);
    });
  }).on('error', (err) => {
    console.error(`[oasis-mcp] Download error: ${err.message}`);
    process.exit(1);
  });
}

download(url, dest);
