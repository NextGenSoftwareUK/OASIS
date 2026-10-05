const { body } = require('express-validator');

const carCreateValidation = [
  body('vehicleRegNumber')
    .isString()
    .withMessage('vehicleRegNumber must be a string')
    .notEmpty()
    .withMessage('vehicleRegNumber name is required'),

  body('vehicleModelYear')
    .isString()
    .withMessage('vehicleModelYear must be a string')
    .notEmpty()
    .withMessage('vehicleModelYear name is required'),

  body('vehicleMake')
    .isString()
    .withMessage('vehicleMake must be a string')
    .notEmpty()
    .withMessage('vehicleMake name is required'),

  body('vehicleModel')
    .isString()
    .withMessage('vehicleModel must be a string')
    .notEmpty()
    .withMessage('vehicleModel name is required'),

  body('vehicleRegNumber')
    .isString()
    .withMessage('vehicleRegNumber must be a string')
    .notEmpty()
    .withMessage('vehicleRegNumber name is required'),

  body('engineNumber')
    .isString()
    .withMessage('engineNumber must be a string')
    .notEmpty()
    .withMessage('engineNumber name is required'),

  body('vehicleColor')
    .isString()
    .withMessage('vehicleColor must be a string')
    .notEmpty()
    .withMessage('vehicleColor name is required'),

  body('insuranceBroker')
    .isString()
    .withMessage('insuranceBroker must be a string')
    .notEmpty()
    .withMessage('insuranceBroker name is required'),

  body('insurancePolicyNumber')
    .isString()
    .withMessage('insurancePolicyNumber must be a string')
    .notEmpty()
    .withMessage('insurancePolicyNumber name is required'),

  body('imagePath')
    .optional()
    .notEmpty()
    .withMessage('imagePath name is required'),

  body('altImagePath')
    .optional()
    .notEmpty()
    .withMessage('altImagePath name is required'),

  body('interiorImagePath')
    .optional()
    .notEmpty()
    .withMessage('interiorImagePath name is required'),

  body('vehicleAddress')
    .notEmpty()
    .withMessage('vehicleAddress name is required'),

  body('state')
    .notEmpty()
    .withMessage('state name is required')
    .isString()
    .withMessage('state must be a string'),

  body('location.latitude')
    .notEmpty()
    .withMessage('latitude  longitude is required')
    .isNumeric()
    .withMessage('Latitude must be a number'),

  body('location.longitude')
    .notEmpty()
    .withMessage('location  longitude is required')
    .isNumeric()
    .withMessage('Longitude must be a number'),
];

const carUpdateValidation = [
  body('vehicleRegNumber')
    .optional()
    .isString()
    .withMessage('vehicleRegNumber must be a string')
    .notEmpty()
    .withMessage('vehicleRegNumber name is required'),

  body('vehicleModelYear')
    .optional()
    .isString()
    .withMessage('vehicleModelYear must be a string')
    .notEmpty()
    .withMessage('vehicleModelYear name is required'),

  body('vehicleMake')
    .optional()
    .isString()
    .withMessage('vehicleMake must be a string')
    .notEmpty()
    .withMessage('vehicleMake name is required'),

  body('vehicleModel')
    .optional()
    .isString()
    .withMessage('vehicleModel must be a string')
    .notEmpty()
    .withMessage('vehicleModel name is required'),

  body('isOfline')
    .optional()
    .isBoolean()
    .withMessage('isOfline must be a boolean')
    .notEmpty()
    .withMessage('isOfline  is required'),

  body('vehicleRegNumber')
    .optional()
    .isString()
    .withMessage('vehicleRegNumber must be a string')
    .notEmpty()
    .withMessage('vehicleRegNumber name is required'),

  body('engineNumber')
    .optional()
    .isString()
    .withMessage('engineNumber must be a string')
    .notEmpty()
    .withMessage('engineNumber name is required'),

  body('vehicleColor')
    .optional()
    .isString()
    .withMessage('vehicleColor must be a string')
    .notEmpty()
    .withMessage('vehicleColor name is required'),

  body('insuranceBroker')
    .optional()
    .isString()
    .withMessage('insuranceBroker must be a string')
    .notEmpty()
    .withMessage('insuranceBroker name is required'),

  body('insurancePolicyNumber')
    .optional()
    .isString()
    .withMessage('insurancePolicyNumber must be a string')
    .notEmpty()
    .withMessage('insurancePolicyNumber name is required'),

  body('imagePath')
    .optional()
    .optional()
    .notEmpty()
    .withMessage('imagePath name is required'),

  body('altImagePath')
    .optional()
    .optional()
    .notEmpty()
    .withMessage('altImagePath name is required'),

  body('interiorImagePath')
    .optional()
    .optional()
    .notEmpty()
    .withMessage('altImagePath name is required'),

  body('vehicleAddress')
    .optional()
    .notEmpty()
    .withMessage('altImagePath name is required'),

  body('state')
    .optional()
    .notEmpty()
    .withMessage('state name is required')
    .isString()
    .withMessage('state must be a string'),

  body('location.latitude')
    .optional()
    .notEmpty()
    .withMessage('latitude  longitude is required')
    .isNumeric()
    .withMessage('Latitude must be a number'),

  body('location.longitude')
    .optional()
    .notEmpty()
    .withMessage('location  longitude is required')
    .isNumeric()
    .withMessage('Longitude must be a number'),
];

module.exports = { carCreateValidation, carUpdateValidation };
