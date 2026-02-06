#!/usr/bin/env node
/**
 * If the admin build (index.html) is missing, run medusa build so the server can start.
 * Fixes Railway deploy when the build phase doesn't preserve .medusa (e.g. cache/root dir).
 */
const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const adminIndex = path.join(process.cwd(), '.medusa', 'server', 'public', 'admin', 'index.html');

if (fs.existsSync(adminIndex)) {
  console.log('Admin build found.');
  process.exit(0);
  return;
}

console.log('Admin build missing, running medusa build...');
process.env.NODE_ENV = process.env.NODE_ENV || 'production';
try {
  execSync('pnpm exec medusa build', {
    stdio: 'inherit',
    cwd: process.cwd(),
    env: { ...process.env, NODE_ENV: 'production' },
  });
  const serverDir = path.join(process.cwd(), '.medusa', 'server');
  execSync('pnpm install --prod', {
    stdio: 'inherit',
    cwd: serverDir,
    env: process.env,
  });
} catch (e) {
  console.error('medusa build failed:', e.message);
  process.exit(1);
}
console.log('Admin build completed.');
process.exit(0);
