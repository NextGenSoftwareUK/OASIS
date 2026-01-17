const express = require('express');
const router = express.Router();

const {
  getGlobalSettings,
  updateGlobalSettings,
  updateCarStatus,
  getpaymentRequest,
  confirmPaymentRequest,
} = require('../controllers/adminController');

const validatorResponse = require('../validators/validatorResponse.js');

const { updateCarStatusValidation } = require('../validators/adminValidation');

router.get('/settings', getGlobalSettings);

router.put('/settings', updateGlobalSettings);

router.get('/request-payments', getpaymentRequest);

router.put('/confirm-payment/:id', confirmPaymentRequest);

router.put(
  '/car-status',
  updateCarStatusValidation,
  validatorResponse,
  updateCarStatus
);

module.exports = router;
