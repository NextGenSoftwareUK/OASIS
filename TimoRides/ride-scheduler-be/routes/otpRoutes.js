const express = require('express');
const router = express.Router();

const authenticateUser = require('../middleware/authMiddleware.js');

const {
  confirmOtp,
  getAllTripDetail,
} = require('../controllers/otpController.js');

router.post('/confirm-otp', authenticateUser, confirmOtp);

router.get('', authenticateUser, getAllTripDetail);
module.exports = router;
