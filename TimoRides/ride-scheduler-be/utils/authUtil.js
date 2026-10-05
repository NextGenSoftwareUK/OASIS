const jwt = require('jsonwebtoken');
require('dotenv').config({ path: '../config/.env' });

// Generate Token - Verification
function generateVerificationToken(email) {
  return jwt.sign({ email: email }, process.env.ACCESS_TOKEN_SECRET, {
    expiresIn: '10m',
  });
}

// Function to generate access token
function generateAccessToken(user) {
  return jwt.sign({ userId: user._id }, process.env.ACCESS_TOKEN_SECRET, {
    expiresIn: '24h',
  });
}

// Function to generate refresh token
function generateRefreshToken(user) {
  return jwt.sign({ userId: user._id }, process.env.REFRESH_TOKEN_SECRET);
}

module.exports = {
  generateAccessToken,
  generateRefreshToken,
  generateVerificationToken,
};
