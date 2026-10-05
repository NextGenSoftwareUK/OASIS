const { body, validationResult } = require('express-validator');

const notficationValidation = [
  body('templateName').notEmpty().withMessage('Template name  is required'),
  body('recipient')
    .notEmpty()
    .withMessage('Email is required')
    .isEmail()
    .withMessage('Invalid email format'),
  body('subject').notEmpty().withMessage('Subject is required'),
];

const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
  next();
};

module.exports = {
  notficationValidation,
  validate,
};
