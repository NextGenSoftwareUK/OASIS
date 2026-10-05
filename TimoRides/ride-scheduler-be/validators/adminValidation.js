const { body } = require('express-validator');
const mongoose = require('mongoose');

const updateCarStatusValidation = [
  // Validate carId
  body('carId')
    .notEmpty()
    .withMessage('Car ID is required')
    .custom((value) => {
      if (!mongoose.Types.ObjectId.isValid(value)) {
        throw new Error('Invalid Car ID format');
      }
      return true;
    }),
];

module.exports = {
  updateCarStatusValidation,
};
