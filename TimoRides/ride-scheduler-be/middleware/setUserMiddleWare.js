// middleware/authMiddleware.js
require('dotenv').config({ path: '../config/.env' });
const jwt = require('jsonwebtoken');
const User = require('../models/userModel');

// Middleware to set user details if token is avialable
async function setUserMiddleWare(req, res, next) {
  // Extract the JWT token from the request headers
  const token = req.headers.authorization;

  // Check if the token is provided
  if (!token) {
    // Call the next middleware or route handler
    req.user = null;
    return next();
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
      req.user = null;
      return next();
    }

    // Attach the user object to the request for further processing
    req.user = user;

    // Call the next middleware or route handler
    next();
  } catch (error) {
    // If token issue use will be passed as null
    req.user = null;
    next();
  }
}

module.exports = setUserMiddleWare;
