// middleware/authMiddleware.js
require('dotenv').config({ path: '../config/.env' });
const jwt = require('jsonwebtoken');
const User = require('../models/userModel');

// Middleware to verify user authentication
async function authenticateUser(req, res, next) {
  // Extract the JWT token from the request headers
  const token = req.headers.authorization;

  // Check if the token is provided
  if (!token) {
    return res.status(401).json({ error: 'Unauthorized - Missing token' });
  }

  try {
    let tokenVal = token;

    // If token starts with Bearer
    if (token.startsWith('Bearer ')) {
      tokenVal = token.slice(7, token.length).trimLeft();
    }

    // Verify the token
    const decodedToken = jwt.verify(tokenVal, process.env.ACCESS_TOKEN_SECRET);

    // Check if the user exists in the database
    const user = await User.findOne({ _id: decodedToken.userId });

    if (!user) {
      return res.status(401).json({ error: 'Unauthorized - User not found' });
    }

    if (user.isVerified === false) {
      return res
        .status(405)
        .json({ error: 'Unauthorized - verify your email' });
    }

    // Attach the user object to the request for further processing
    req.user = user;

    // Call the next middleware or route handler
    next();
  } catch (error) {
    if (error.name === 'TokenExpiredError') {
      // Token has expired
      return res
        .status(401)
        .json({ error: 'Unauthorized - Token expired', isExpired: true });
    }
    // Other JWT verification errors
    return res.status(401).json({ error: 'Unauthorized - Invalid token' });
  }
}

module.exports = authenticateUser;
