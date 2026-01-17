const express = require('express');
const userRoutes = require('./userRoutes');
const carRoutes = require('./carRoutes');
const authRoutes = require('./authRoutes');
const notificationRoutes = require('./notificationRoutes');
const distanceRoutes = require('./distanceRoutes');
const uploadRoutes = require('./uploadRoutes.js');
const bookingRoutes = require('./bookingRoutes.js');
const tripRoutes = require('./otpRoutes.js');
const adminRoutes = require('./adminRoutes.js');
const driverRoutes = require('./driverRoutes');
const driverSignalRoutes = require('./driverSignalRoutes');
const metricsRoutes = require('./metricsRoutes');
const healthRoutes = require('./healthRoutes');
const webhookRoutes = require('./webhookRoutes');
const watiRoutes = require('./watiRoutes');
const authenticateUser = require('../middleware/authMiddleware.js');
const authorizeUser = require('../middleware/authorizationMiddleware.js');
const {
  authLimiter,
  bookingLimiter,
} = require('../middleware/rateLimiters');

const router = express.Router();

router.use('/health', healthRoutes);
router.use('/', webhookRoutes); // Webhooks don't need /api prefix
router.use('/api/wati', watiRoutes);
router.use('/api/distance', distanceRoutes);
router.use('/api/notification', notificationRoutes);
router.use('/api/auth', authLimiter, authRoutes);
router.use('/api/trips', tripRoutes);
router.use('/api', carRoutes);
router.use('/api/bookings', bookingLimiter, bookingRoutes);
router.use('/api', userRoutes);
router.use('/api', driverRoutes);
router.use('/api', driverSignalRoutes);
router.use('/api/metrics', metricsRoutes);
router.use('/api/admin', authorizeUser(['admin']), adminRoutes);
router.use('/api', authenticateUser, uploadRoutes);

module.exports = router;
