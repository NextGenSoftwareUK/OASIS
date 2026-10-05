const express = require('express');
const authenticateUser = require('../middleware/authMiddleware');

const {
  updateLocation,
  updateStatus,
  getStatus,
} = require('../controllers/driverController');

const router = express.Router();

router.patch(
  '/drivers/:driverId/location',
  authenticateUser,
  updateLocation
);
router.patch('/drivers/:driverId/status', authenticateUser, updateStatus);
router.get('/drivers/:driverId/status', authenticateUser, getStatus);

module.exports = router;


