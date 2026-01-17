const jwt = require('jsonwebtoken');
require('dotenv').config({ path: '../config/.env' });

// Generate Token - Verification
function generateIdToken(params) {
  return jwt.sign(params, process.env.VALIDATION_TOKEN_SECRET, {
    expiresIn: '10m',
  });
}

module.exports = generateIdToken;
