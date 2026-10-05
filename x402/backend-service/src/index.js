/**
 * @oasis-web4/x402-service
 * 
 * Automatic revenue distribution for NFTs on Solana
 */

const X402Service = require('./X402Service');
const { FileStorage, MongoStorage } = require('./storage');
const X402PaymentDistributor = require('./distributor/X402PaymentDistributor');

module.exports = {
  X402Service,
  FileStorage,
  MongoStorage,
  X402PaymentDistributor,
  
  // Convenience exports
  default: X402Service
};

