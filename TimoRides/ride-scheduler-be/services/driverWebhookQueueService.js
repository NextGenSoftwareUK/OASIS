const DriverWebhookQueue = require('../models/driverWebhookQueue');

const DEFAULT_MAX_ATTEMPTS =
  Number(process.env.DRIVER_SIGNAL_MAX_ATTEMPTS) || 3;

function getBackoffDelayMs(attemptNumber = 1) {
  const base = Number(process.env.DRIVER_SIGNAL_BACKOFF_BASE_MS) || 5000;
  return base * Math.pow(2, Math.max(0, attemptNumber - 1));
}

async function enqueueWebhookEvent({
  eventType = 'telemetry',
  source = 'pathpulse',
  signature,
  timestamp,
  payload,
  rawPayloadHash,
  ipAddress,
  traceId,
  maxAttempts = DEFAULT_MAX_ATTEMPTS,
}) {
  return DriverWebhookQueue.create({
    eventType,
    source,
    signature,
    timestamp,
    payload,
    rawPayloadHash,
    ipAddress,
    nextAttemptAt: new Date(),
    traceId,
    maxAttempts,
  });
}

async function claimNextPending() {
  const now = new Date();

  return DriverWebhookQueue.findOneAndUpdate(
    {
      status: { $in: ['pending', 'failed'] },
      nextAttemptAt: { $lte: now },
    },
    {
      $set: { status: 'processing', processingStartedAt: now },
      $inc: { attempts: 1 },
    },
    {
      sort: { nextAttemptAt: 1, createdAt: 1 },
      new: true,
    }
  )
    .lean()
    .exec();
}

async function markCompleted(id) {
  await DriverWebhookQueue.findByIdAndUpdate(id, {
    $set: {
      status: 'completed',
      processedAt: new Date(),
      lastError: null,
    },
  }).exec();
}

async function markFailed(id, error) {
  const doc = await DriverWebhookQueue.findById(id).exec();

  if (!doc) {
    return 0;
  }

  const attempts = doc.attempts || 0;
  const remainingAttempts = (doc.maxAttempts || DEFAULT_MAX_ATTEMPTS) - attempts;

  if (remainingAttempts <= 0) {
    await DriverWebhookQueue.findByIdAndUpdate(id, {
      $set: {
        status: 'dead-letter',
        lastError: error?.message || String(error),
        processedAt: new Date(),
      },
    }).exec();

    return 0;
  }

  const delayMs = getBackoffDelayMs(attempts);

  await DriverWebhookQueue.findByIdAndUpdate(id, {
    $set: {
      status: 'failed',
      lastError: error?.message || String(error),
      nextAttemptAt: new Date(Date.now() + delayMs),
    },
  }).exec();

  return remainingAttempts;
}

async function resetProcessingStuckEntries({
  timeoutMs = 60000,
} = {}) {
  const cutoff = new Date(Date.now() - timeoutMs);

  await DriverWebhookQueue.updateMany(
    {
      status: 'processing',
      processingStartedAt: { $lt: cutoff },
    },
    {
      $set: {
        status: 'failed',
        lastError: 'Processing timeout',
        nextAttemptAt: new Date(),
      },
    }
  ).exec();
}

module.exports = {
  enqueueWebhookEvent,
  claimNextPending,
  markCompleted,
  markFailed,
  resetProcessingStuckEntries,
};

