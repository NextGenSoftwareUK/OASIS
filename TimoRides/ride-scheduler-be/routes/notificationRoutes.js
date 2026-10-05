const express = require('express');
const router = express.Router();
const {
  emailNotification,
  sendUserOtp,
  verifyUserOtp,
} = require('../controllers/notificationController');
const {
  notficationValidation,
  validate,
} = require('../validators/notificationValidation');

router.post('/email', notficationValidation, validate, emailNotification);

// SendOtp
router.post('/sendUserOtp', sendUserOtp);

// SendOtp
router.post('/verifyUserOtp', verifyUserOtp);

module.exports = router;
