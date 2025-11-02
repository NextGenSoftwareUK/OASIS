/**
 * Storage Adapters Export
 * 
 * All available storage adapters for x402 service
 */

const FileStorage = require('./FileStorage');
const MongoStorage = require('./MongoStorage');

module.exports = {
  FileStorage,
  MongoStorage
};

