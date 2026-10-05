const { body, validationResult } = require('express-validator');
const mongoose = require('mongoose');

// Validation middleware
const bookingValidation = [
  body('car').notEmpty().withMessage('Car id is required'),
  body('tripAmount').notEmpty().withMessage('Trip amount is required'),
  body('isCash').isBoolean().withMessage('isCash must be a boolean value'),
  body('departureTime').notEmpty().withMessage('Departure time is required'),
  body('phoneNumber')
    .notEmpty()
    .withMessage('Phone number is required')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('Phone number must be between 10 and 15 digits'),

  body('email').notEmpty().withMessage('email  is required'),
  body('fullName').notEmpty().withMessage('fullName  is required'),

  body('bookingType').notEmpty().withMessage('bookingType  is required'),
  body('sourceLocation.address')
    .notEmpty()
    .withMessage('Source location address is required'),
  body('sourceLocation.latitude')
    .notEmpty()
    .withMessage('Source location latitude is required')
    .isNumeric()
    .withMessage('Latitude must be a number'),
  body('sourceLocation.longitude')
    .notEmpty()
    .withMessage('Source location longitude is required')
    .isNumeric()
    .withMessage('Longitude must be a number'),
  body('destinationLocation.address')
    .notEmpty()
    .withMessage('Destination location address is required'),
  body('destinationLocation.latitude')
    .notEmpty()
    .withMessage('Destination location latitude is required')
    .isNumeric()
    .withMessage('Latitude must be a number'),
  body('destinationLocation.longitude')
    .notEmpty()
    .withMessage('Destination location longitude is required')
    .isNumeric()
    .withMessage('Longitude must be a number'),
];

// Validation middleware
const reBookingValidation = [
  body('car')
    .notEmpty()
    .withMessage('Car ID is required')
    .custom((value) => {
      if (!mongoose.Types.ObjectId.isValid(value)) {
        throw new Error('Invalid Car ID format');
      }
      return true;
    }),

  body('bookingId')
    .notEmpty()
    .withMessage('Booking ID is required')
    .custom((value) => {
      if (!mongoose.Types.ObjectId.isValid(value)) {
        throw new Error('Invalid Booking Id format');
      }
      return true;
    }),
];

const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
  next();
};

module.exports = {
  bookingValidation,
  reBookingValidation,
  validate,
};
