/**
 * storage-utils.js
 *
 * Thin wrapper around the configured storage adapter so modules that still
 * expect the legacy utility helper can continue to function.
 * Defaults to the file-based storage that ships with the standalone service.
 */

const path = require('path');
const FileStorage = require('./FileStorage');

// Reuse file storage by default. In future we can inject Mongo via env vars.
const defaultStorage = new FileStorage(path.join(__dirname, '../data'));

async function storeX402Config(nftMintAddress, x402Config) {
  return defaultStorage.storeConfig(nftMintAddress, x402Config);
}

async function getX402Config(nftMintAddress) {
  return defaultStorage.getConfig(nftMintAddress);
}

async function recordX402Distribution(distribution) {
  return defaultStorage.recordDistribution(distribution);
}

async function getX402Distributions(nftMintAddress, limit = 10) {
  return defaultStorage.getDistributions(nftMintAddress, limit);
}

module.exports = {
  storeX402Config,
  getX402Config,
  recordX402Distribution,
  getX402Distributions,
};


