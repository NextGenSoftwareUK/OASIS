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
const authenticateUser = require('../middleware/authMiddleware.js');
const authorizeUser = require('../middleware/authorizationMiddleware.js');

const router = express.Router();

router.use('/api/distance', distanceRoutes);
router.use('/api/notification', notificationRoutes);
router.use('/api/auth', authRoutes);
router.use('/api/trips', tripRoutes);
router.use('/api', carRoutes);
router.use('/api/bookings', bookingRoutes);
router.use('/api', userRoutes);
router.use('/api/admin', authorizeUser(['admin']), adminRoutes);
router.use('/api', authenticateUser, uploadRoutes);

module.exports = router;
