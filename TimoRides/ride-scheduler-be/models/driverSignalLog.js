const mongoose = require('mongoose');

const driverSignalLogSchema = new mongoose.Schema(
  {
    driverId: {
      type: mongoose.Schema.Types.ObjectId,
      ref: 'Driver',
      required: true,
    },
    bookingId: {
      type: mongoose.Schema.Types.ObjectId,
      ref: 'Booking',
      required: true,
    },
    action: {
      type: String,
      required: true,
      lowercase: true,
    },
    source: {
      type: String,
      default: 'unknown',
    },
    payload: {
      type: mongoose.Schema.Types.Mixed,
      default: {},
    },
    traceId: {
      type: String,
    },
  },
  { timestamps: true }
);

driverSignalLogSchema.index({ driverId: 1, createdAt: -1 });
driverSignalLogSchema.index({ bookingId: 1, createdAt: -1 });

module.exports = mongoose.model(
  'DriverSignalLog',
  driverSignalLogSchema,
  'driver_signal_logs'
);

