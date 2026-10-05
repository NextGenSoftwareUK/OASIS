const express = require('express');
const {
  handleDriverAction,
  handlePathPulseWebhook,
  handleDriverLocationUpdate,
} = require('../controllers/driverSignalController');
const serviceTokenMiddleware = require('../middleware/serviceTokenMiddleware');
const {
  driverSignalLimiter,
  webhookLimiter,
} = require('../middleware/rateLimiters');

const router = express.Router();

router.post(
  '/driver-actions',
  driverSignalLimiter,
  serviceTokenMiddleware,
  handleDriverAction
);
router.post(
  '/driver-location',
  driverSignalLimiter,
  serviceTokenMiddleware,
  handleDriverLocationUpdate
);
router.post(
  '/driver-webhooks/pathpulse',
  webhookLimiter,
  handlePathPulseWebhook
);

module.exports = router;

