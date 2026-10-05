// Input validation for user-related requests
// userValidator.js

const { body, validationResult } = require('express-validator');

const signUpValidation = [
  body('fullName').notEmpty().withMessage('Full name is required'),
  body('email')
    .notEmpty()
    .withMessage('Email is required')
    .isEmail()
    .withMessage('Invalid email format'),

  body('phone')
    .notEmpty()
    .withMessage('phone must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('phone must be between 10 and 15 digits'),

  body('password')
    .notEmpty()
    .withMessage('Password is required')
    .isLength({ min: 8 })
    .withMessage('Password must be at least 8 characters long'),
  body('userType')
    .notEmpty()
    .withMessage('User type is Empty')
    .isIn(['user', 'driver'])
    .withMessage('Invalid userType [user , driver]'),
];

// Login Validataation
const loginValidation = [
  body('email')
    .notEmpty()
    .withMessage('Email is required')
    .isEmail()
    .withMessage('Invalid email format'),

  body('password')
    .notEmpty()
    .withMessage('Password is required')
    .isLength({ min: 8 })
    .withMessage('Password must be at least 8 characters long'),
];

// refresh Token
const refreshTokenValidation = [
  body('refreshToken').notEmpty().withMessage('Refresh token is required'),
];

// verify Token
const verifyhTokenValidation = [
  body('token').notEmpty().withMessage('Token  is required'),
];

// Login Validataation
const reSendVerificationTokenValidatetion = [
  body('email')
    .notEmpty()
    .withMessage('Email is required')
    .isEmail()
    .withMessage('Invalid email format'),
];

const passwordUpdateValidation = [
  body('prevPassword').notEmpty().withMessage('prev password is required'),
  body('newPassword').notEmpty().withMessage('New password  is required'),
];

const generatePasswordChangeLinkValidation = [
  body('email')
    .notEmpty()
    .withMessage('Email is required')
    .isEmail()
    .withMessage('Invalid email format'),
];

// Login Validataation
const ChangePasswordValidation = [
  body('token').notEmpty().withMessage('token is required'),

  body('newPassword').notEmpty().withMessage('New password  is required'),
];

const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
  next();
};

module.exports = {
  signUpValidation,
  loginValidation,
  refreshTokenValidation,
  verifyhTokenValidation,
  reSendVerificationTokenValidatetion,
  passwordUpdateValidation,
  generatePasswordChangeLinkValidation,
  ChangePasswordValidation,
  validate,
};
