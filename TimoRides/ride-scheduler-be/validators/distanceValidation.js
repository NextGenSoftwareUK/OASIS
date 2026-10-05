const { body, validationResult } = require('express-validator');

// Validation middleware
const distanceValidation = [
  body('sourceCoordinate.latitude')
    .isNumeric()
    .withMessage('sourceCoordinate.latitude must be Numeric')
    .notEmpty()
    .withMessage('sourceCoordinate.latitude is required'),
  body('sourceCoordinate.longitude')
    .isNumeric()
    .withMessage('sourceCoordinate.longitude must be Numeric')
    .notEmpty()
    .withMessage('sourceCoordinate.longitude is required'),
  body('destinationCoordinate.latitude')
    .isNumeric()
    .withMessage('destinationCoordinate.latitude must be Numeric')
    .notEmpty()
    .withMessage('destinationCoordinate.longitude is required'),
  body('destinationCoordinate.longitude')
    .isNumeric()
    .withMessage('destinationCoordinate.longitude must be Numeric')
    .notEmpty()
    .withMessage('sourceCoordinate.longitude is required'),
  body('amountPerKilo')
    .custom((value) => {
      if (value <= 0) {
        throw new Error(
          'amountPerKilo  must be float and greater than 0 eg: 0.05'
        );
      }
      return true;
    })
    .notEmpty()
    .withMessage('amountPerKilo is required'),
];

const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
  next();
};

module.exports = {
  distanceValidation,
  validate,
};
