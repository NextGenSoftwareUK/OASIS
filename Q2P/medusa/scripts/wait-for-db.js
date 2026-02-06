#!/usr/bin/env node
/**
 * Wait for Postgres to be reachable before starting Medusa.
 * Fixes "Pg connection failed" / KnexTimeoutError when Postgres is slow to accept connections (e.g. Railway).
 * Usage: node scripts/wait-for-db.js
 * Exits 0 when DB is ready, 1 after maxAttempts (default 30, every 2s = 60s total).
 */

const { Client } = require('pg');

const maxAttempts = parseInt(process.env.DB_WAIT_MAX_ATTEMPTS || '30', 10);
const delayMs = parseInt(process.env.DB_WAIT_DELAY_MS || '2000', 10);
const url = process.env.DATABASE_URL || process.env.DATABASE_PUBLIC_URL;

if (!url) {
  console.error('wait-for-db: DATABASE_URL (or DATABASE_PUBLIC_URL) not set');
  process.exit(1);
}

async function tryConnect() {
  const client = new Client({
    connectionString: url,
    connectionTimeoutMillis: 10000,
  });
  try {
    await client.connect();
    await client.query('SELECT 1');
    await client.end();
    return true;
  } catch (e) {
    return false;
  }
}

async function main() {
  for (let i = 0; i < maxAttempts; i++) {
    if (i > 0) {
      console.log(`Pg connection attempt ${i + 1}/${maxAttempts} in 2s...`);
      await new Promise((r) => setTimeout(r, delayMs));
    }
    if (await tryConnect()) {
      console.log('Database is ready.');
      process.exit(0);
    }
  }
  console.error('Database did not become ready in time.');
  process.exit(1);
}

main();
