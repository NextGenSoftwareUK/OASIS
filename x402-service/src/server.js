/**
 * Standalone x402 Service Server
 * 
 * Start this file directly to run x402 as a standalone service
 */

require('dotenv').config();
const { X402Service, FileStorage, MongoStorage } = require('./index');
const path = require('path');

async function startServer() {
  console.log('🚀 Starting OASIS x402 Service...\n');

  // Determine storage type
  const storageType = process.env.X402_STORAGE || 'file';
  let storage;

  if (storageType === 'mongodb') {
    console.log('📦 Using MongoDB storage');
    storage = new MongoStorage({
      url: process.env.MONGODB_URL,
      database: process.env.MONGODB_DATABASE
    });
  } else {
    console.log('📦 Using file storage');
    const dataDir = process.env.X402_DATA_DIR || path.join(__dirname, '../data');
    storage = new FileStorage(dataDir);
  }

  // Create and start service
  const service = new X402Service({
    storage,
    solanaRpcUrl: process.env.SOLANA_RPC_URL,
    useMockData: process.env.X402_USE_MOCK_DATA !== 'false',
    webhookSecret: process.env.X402_WEBHOOK_SECRET
  });

  const port = parseInt(process.env.X402_PORT || '4000');
  const host = process.env.X402_HOST || '0.0.0.0';

  await service.start({ port, host });

  // Handle graceful shutdown
  process.on('SIGTERM', () => {
    console.log('\n👋 Shutting down gracefully...');
    process.exit(0);
  });

  process.on('SIGINT', () => {
    console.log('\n👋 Shutting down gracefully...');
    process.exit(0);
  });
}

startServer().catch((error) => {
  console.error('❌ Failed to start server:', error);
  process.exit(1);
});

