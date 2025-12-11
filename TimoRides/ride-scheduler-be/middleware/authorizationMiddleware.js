const jwt = require('jsonwebtoken');
const User = require('../models/userModel');
require('dotenv').config({ path: '../config/.env' });

const authorizeUser = (roles) => {
  return async (req, res, next) => {
    const token = req.headers.authorization;

    if (!token) {
      return res
        .status(401)
        .json({ message: 'Unauthorized: Token not provided' });
    }

    try {
      let tokenVal = token;

      if (token.startsWith('Bearer ')) {
        tokenVal = token.slice(7, token.length).trimLeft();
      }

      const decodedToken = jwt.verify(
        tokenVal,
        process.env.ACCESS_TOKEN_SECRET
      );

      const user = await User.findById(decodedToken.userId);

      if (!user) {
        return res.status(401).json({ error: 'Unauthorized - User not found' });
      }

      if (roles.includes(user.role)) {
        req.user = user;
        next();
      } else {
        return res.status(403).json({
          message:
            'Forbidden: You do not have permission to access this resource',
        });
      }
    } catch (error) {
      if (error.name === 'TokenExpiredError') {
        return res
          .status(401)
          .json({ error: 'Unauthorized - Token expired', isExpired: true });
      }
      return res.status(401).json({ error: 'Unauthorized - Invalid token' });
    }
  };
};

module.exports = authorizeUser;
