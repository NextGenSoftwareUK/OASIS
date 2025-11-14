// routes/userRoutes.js

const express = require('express');
const router = express.Router();
const authenticateUser = require('../middleware/authMiddleware');

const {
  login,
  signup,
  refreshToken,
  verifyToken,
  reSendVerificationToken,
  updatePassword,
  generateChangePasswordLink,
  changePassword,
} = require('../controllers/authController');

const {
  signUpValidation,
  loginValidation,
  refreshTokenValidation,
  verifyhTokenValidation,
  reSendVerificationTokenValidatetion,
  passwordUpdateValidation,
  generatePasswordChangeLinkValidation,
  ChangePasswordValidation,
  validate,
} = require('../validators/authValidation');

// Login route
router.post('/login', loginValidation, validate, login);

// refresh token
router.post('/refresh-token', refreshTokenValidation, validate, refreshToken);

// Verify token
router.post('/verify-token', verifyhTokenValidation, validate, verifyToken);

// Verify token
router.post(
  '/send-email-verify-token',
  reSendVerificationTokenValidatetion,
  validate,
  reSendVerificationToken
);

// Update password
router.post(
  '/update-password',
  authenticateUser,
  passwordUpdateValidation,
  validate,
  updatePassword
);

// generate password link  password
router.post(
  '/generate-change-password-link',
  generatePasswordChangeLinkValidation,
  validate,
  generateChangePasswordLink
);

// Signup route
router.post('/signup', signUpValidation, validate, signup);

// Change password
router.post(
  '/change-password',
  ChangePasswordValidation,
  validate,
  changePassword
);

module.exports = router;
