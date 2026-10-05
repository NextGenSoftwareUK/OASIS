const { body, validationResult } = require('express-validator');

const uploadImageValidation = [
  body('filename').notEmpty().withMessage('filename is required'),
  body('blob').notEmpty().withMessage('blob is required'),
];

const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
  next();
};

module.exports = {
  uploadImageValidation,
  validate,
};
