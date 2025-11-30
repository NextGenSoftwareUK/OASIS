const mongoose = require('mongoose');

const driverWebhookQueueSchema = new mongoose.Schema(
  {
    eventType: {
      type: String,
      default: 'telemetry',
    },
    source: {
      type: String,
      default: 'pathpulse',
    },
    payload: {
      type: mongoose.Schema.Types.Mixed,
      required: true,
    },
    status: {
      type: String,
      enum: ['pending', 'processing', 'completed', 'failed', 'dead-letter'],
      default: 'pending',
    },
    attempts: {
      type: Number,
      default: 0,
    },
    maxAttempts: {
      type: Number,
      default: 3,
    },
    nextAttemptAt: {
      type: Date,
      default: () => new Date(),
    },
    processingStartedAt: {
      type: Date,
    },
    lastError: {
      type: String,
    },
    signature: {
      type: String,
    },
    timestamp: {
      type: String,
    },
    rawPayloadHash: {
      type: String,
    },
    ipAddress: {
      type: String,
    },
    traceId: {
      type: String,
    },
    processedAt: {
      type: Date,
    },
  },
  { timestamps: true }
);

driverWebhookQueueSchema.index({ status: 1, createdAt: 1 });
driverWebhookQueueSchema.index({ nextAttemptAt: 1, status: 1 });
driverWebhookQueueSchema.index(
  { processedAt: 1 },
  { expireAfterSeconds: 172800, partialFilterExpression: { processedAt: { $exists: true } } }
);

module.exports = mongoose.model(
  'DriverWebhookQueue',
  driverWebhookQueueSchema,
  'driver_webhook_queue'
);

