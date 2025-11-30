require('dotenv').config({ path: `${__dirname}/../config/.env` });

const mongoose = require('mongoose');
const driverWebhookQueueService = require('../services/driverWebhookQueueService');
const driverSignalService = require('../services/driverSignalService');
const driverSignalMetrics = require('../utils/driverSignalMetrics');

const POLL_INTERVAL_MS =
  Number(process.env.DRIVER_SIGNAL_WORKER_INTERVAL_MS) || 2000;
const MAX_BATCH =
  Number(process.env.DRIVER_SIGNAL_WORKER_BATCH_SIZE) || 5;
const PROCESSING_TIMEOUT_MS =
  Number(process.env.DRIVER_SIGNAL_WORKER_TIMEOUT_MS) || 60000;

let shuttingDown = false;

async function processQueueEntry(entry) {
  const start = Date.now();
  try {
    if (entry.eventType === 'telemetry') {
      const telemetry =
        entry.payload?.telemetry || entry.payload?.location || entry.payload;
      const driverId =
        entry.payload?.driverId || entry.payload?.driverExternalId;

      if (!driverId) {
        throw new Error('driverId missing in telemetry payload');
      }

      await driverSignalService.handleDriverLocationUpdate({
        driverId,
        bookingId: entry.payload?.bookingId || entry.payload?.bookingExternalId,
        location: {
          latitude: telemetry.latitude,
          longitude: telemetry.longitude,
          speed: telemetry.speed,
          bearing: telemetry.bearing,
        },
        source: entry.source,
        traceId: entry.traceId,
      });
    } else {
      const driverId =
        entry.payload?.driverId || entry.payload?.driverExternalId;
      const bookingId =
        entry.payload?.bookingId || entry.payload?.bookingExternalId;
      const actionType =
        entry.payload?.action?.type || entry.payload?.eventType || entry.eventType;

      if (!driverId || !bookingId) {
        throw new Error('driverId or bookingId missing in action payload');
      }

      await driverSignalService.handleDriverAction({
        driverId,
        bookingId,
        action: actionType,
        source: entry.source,
        meta: entry.payload?.action?.meta || entry.payload?.meta,
        traceId: entry.traceId,
      });
    }

    await driverWebhookQueueService.markCompleted(entry._id);
    driverSignalMetrics.recordQueueProcessed({
      source: entry.source,
      eventType: entry.eventType,
      latencyMs: Date.now() - start,
    });
  } catch (error) {
    const remaining = await driverWebhookQueueService.markFailed(
      entry._id,
      error
    );
    driverSignalMetrics.recordQueueFailure({
      source: entry.source,
      eventType: entry.eventType,
    });
    console.error(
      `[driver-signal-worker] failed processing ${entry._id}: ${error.message}`
    );

    if (remaining <= 0) {
      driverSignalMetrics.recordQueueDeadLetter({
        source: entry.source,
        eventType: entry.eventType,
      });
    }
  }
}

async function processBatch() {
  let processed = 0;

  while (processed < MAX_BATCH && !shuttingDown) {
    const entry = await driverWebhookQueueService.claimNextPending();
    if (!entry) {
      break;
    }

    // eslint-disable-next-line no-await-in-loop
    await processQueueEntry(entry);
    processed += 1;
  }
}

async function startWorker() {
  const mongoDbUrl = process.env.Database_Url;
  if (!mongoDbUrl) {
    throw new Error('Database_Url not configured');
  }

  await mongoose.connect(mongoDbUrl);
  console.log('[driver-signal-worker] Connected to MongoDB');

  const loop = async () => {
    if (shuttingDown) {
      return;
    }

    await driverWebhookQueueService.resetProcessingStuckEntries({
      timeoutMs: PROCESSING_TIMEOUT_MS,
    });
    await processBatch();

    setTimeout(loop, POLL_INTERVAL_MS);
  };

  loop();
}

process.on('SIGINT', async () => {
  console.log('\n[driver-signal-worker] Shutting down...');
  shuttingDown = true;
  await mongoose.disconnect();
  process.exit(0);
});

startWorker().catch((error) => {
  console.error('[driver-signal-worker] Fatal error', error);
  process.exit(1);
});


