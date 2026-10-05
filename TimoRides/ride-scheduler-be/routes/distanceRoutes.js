const express = require('express');
const router = express.Router();

const { calculateDistanceAmount } = require('../controllers/distantController');

const {
  distanceValidation,
  validate,
} = require('../validators/distanceValidation');

router.post(
  '/calculate-distance-amount',
  distanceValidation,
  validate,
  calculateDistanceAmount
);

module.exports = router;
