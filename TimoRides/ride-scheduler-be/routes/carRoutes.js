// routes/rideRoutes.js

const express = require('express');
const router = express.Router();

const validatorResponse = require('../validators/validatorResponse.js');

const {
  getCars,
  createCar,
  updateCar,
  getCar,
  getCurrentCar,
  getCarsProximity,
} = require('../controllers/carController');

const authenticateUser = require('../middleware/authMiddleware.js');

const {
  carCreateValidation,
  carUpdateValidation,
} = require('../validators/carValidation.js');

// Get avialbe cars based on location and distance
router.get('/cars/proximity', getCarsProximity);

// Get car
router.get('/cars/:carId', getCar);

router.post(
  '/cars',
  authenticateUser,
  carCreateValidation,
  validatorResponse,
  createCar
);

router.put(
  '/cars/:driverId',
  authenticateUser,
  carUpdateValidation,
  validatorResponse,
  updateCar
);

// Define routes for ride-related endpoints
router.get('/cars', authenticateUser, getCars);

// Do not move ths route upward
router.get('/cars/current-car/:driverId', authenticateUser, getCurrentCar);

// Add more routes as needed

module.exports = router;
