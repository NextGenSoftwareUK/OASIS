const mongoose = require('mongoose');
const Booking = require('../models/bookingModal');
const DriverWebhookQueue = require('../models/driverWebhookQueue');
const driverSignalMetrics = require('../utils/driverSignalMetrics');

const startedAt = new Date();

function resolveMongoStatus() {
  switch (mongoose.connection.readyState) {
    case 0:
      return 'disconnected';
    case 1:
      return 'connected';
    case 2:
      return 'connecting';
    case 3:
      return 'disconnecting';
    default:
      return 'unknown';
  }
}

async function getHealth(req, res) {
  try {
    const [pendingBookings, cashPaymentsDue, queueDepth] = await Promise.all([
      Booking.countDocuments({ status: 'pending' }),
      Booking.countDocuments({
        status: { $in: ['accepted', 'started'] },
        'payment.status': { $in: ['unpaid', 'pending'] },
      }),
      DriverWebhookQueue.countDocuments({
        status: { $in: ['pending', 'processing'] },
      }),
    ]);

    const mongoStatus = resolveMongoStatus();

    res.json({
      status:
        mongoStatus === 'connected' && queueDepth < 1000 ? 'ok' : 'degraded',
      timestamp: new Date().toISOString(),
      uptimeSeconds: process.uptime(),
      startedAt,
      mongo: {
        status: mongoStatus,
        host: mongoose.connection.host,
      },
      metrics: {
        pendingBookings,
        pendingPayments: cashPaymentsDue,
        driverWebhookQueueDepth: queueDepth,
        driverSignal: driverSignalMetrics.getSnapshot(),
      },
      version: process.env.GIT_SHA || null,
    });
  } catch (error) {
    res.status(500).json({
      status: 'error',
      message: error.message,
    });
  }
}

module.exports = {
  getHealth,
};

