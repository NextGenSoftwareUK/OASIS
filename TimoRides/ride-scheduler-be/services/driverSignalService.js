const crypto = require('crypto');
const Driver = require('../models/driverModel');
const DriverSignalLog = require('../models/driverSignalLog');
const rideService = require('./rideService');
const driverService = require('./driverService');
const driverWebhookQueueService = require('./driverWebhookQueueService');
const driverSignalMetrics = require('../utils/driverSignalMetrics');

const SUPPORTED_ACTIONS = new Set([
  'accept',
  'reject',
  'start',
  'pause',
  'resume',
  'complete',
  'cancel',
]);

function normalizeAction(action = '') {
  return action.toLowerCase();
}

async function handleDriverAction(payload = {}) {
  const {
    driverId,
    bookingId,
    action,
    source = 'unknown',
    meta = {},
    traceId,
  } = payload;

  let normalizedAction = 'unknown';
  const startTime = Date.now();

  try {
    if (!driverId || !bookingId || !action) {
      const error = new Error('driverId, bookingId and action are required');
      error.statusCode = 400;
      throw error;
    }

    const driver = await Driver.findById(driverId).lean();

    if (!driver) {
      const error = new Error('Driver not found');
      error.statusCode = 404;
      throw error;
    }

    const actor = {
      id: driverId,
      role: 'driver',
      fullName: driver.fullName || 'Driver',
      email: driver.email,
    };

    normalizedAction = normalizeAction(action);

    if (!SUPPORTED_ACTIONS.has(normalizedAction)) {
      const error = new Error(`Unsupported driver action: ${action}`);
      error.statusCode = 400;
      throw error;
    }

    await DriverSignalLog.create({
      driverId,
      bookingId,
      action: normalizedAction,
      source,
      payload: meta,
      traceId,
    });

    let booking = null;

    switch (normalizedAction) {
      case 'accept':
        booking = await rideService.acceptBooking(bookingId, driverId, {
          trxId: meta?.trxId,
          actor,
          traceId,
          source,
        });
        break;
      case 'start':
        booking = await rideService.startRide(bookingId, driverId);
        break;
      case 'complete':
        booking = await rideService.completeRide(bookingId, driverId, {
          paymentStatus: meta?.paymentStatus,
          paymentNotes: meta?.paymentNotes,
          actor,
          traceId,
          source,
        });
        break;
      case 'cancel':
      case 'reject':
        {
          booking = await rideService.cancelRide(
            bookingId,
            {
              id: driverId,
              role: 'driver',
              fullName: driver?.fullName || 'Driver',
              email: driver?.email,
            },
            meta?.reason ||
              (normalizedAction === 'reject'
                ? 'Rejected by driver'
                : 'Cancelled by driver')
          );
        }
        break;
      case 'pause':
      case 'resume':
        // Placeholder: a future enhancement will map these to ride states.
        break;
      default:
        break;
    }

    if (meta?.coordinates) {
      const { latitude, longitude, speed, bearing } = meta.coordinates;

      if (
        typeof latitude === 'number' &&
        typeof longitude === 'number' &&
        !Number.isNaN(latitude) &&
        !Number.isNaN(longitude)
      ) {
        await driverService.updateDriverLocation(driverId, {
          latitude,
          longitude,
          speed,
          bearing,
        });
      }
    }

    const driverSnapshot = await driverService.getDriverSnapshot(driverId);

    driverSignalMetrics.recordAction({
      source,
      action: normalizedAction,
      status: 'success',
      latencyMs: Date.now() - startTime,
    });

    return { booking, driverSnapshot };
  } catch (error) {
    driverSignalMetrics.recordAction({
      source,
      action: normalizedAction || action || 'unknown',
      status: 'failed',
    });
    throw error;
  }
}

async function handleDriverLocationUpdate(payload = {}) {
  const { driverId, location = {}, source = 'unknown', traceId, bookingId } =
    payload;

  if (!driverId) {
    const error = new Error('driverId is required');
    error.statusCode = 400;
    throw error;
  }

  if (
    typeof location.latitude !== 'number' ||
    typeof location.longitude !== 'number'
  ) {
    const error = new Error(
      'location.latitude and location.longitude must be numbers'
    );
    error.statusCode = 400;
    throw error;
  }

  const startTime = Date.now();

  try {
    await DriverSignalLog.create({
      driverId,
      bookingId,
      action: 'location_update',
      source,
      payload: location,
      traceId,
    });

    const snapshot = await driverService.updateDriverLocation(driverId, {
      latitude: location.latitude,
      longitude: location.longitude,
      speed: location.speed,
      bearing: location.bearing,
    });

    driverSignalMetrics.recordLocationUpdate({
      status: 'success',
      latencyMs: Date.now() - startTime,
    });

    return snapshot;
  } catch (error) {
    driverSignalMetrics.recordLocationUpdate({
      status: 'failed',
    });
    throw error;
  }
}

function verifyPathPulseSignature(rawBody, signature, timestamp) {
  const secret = process.env.PATHPULSE_WEBHOOK_SECRET;

  if (!secret) {
    const error = new Error('PathPulse secret not configured');
    error.statusCode = 500;
    throw error;
  }

  if (!signature || !timestamp) {
    const error = new Error('Missing PathPulse signature headers');
    error.statusCode = 401;
    throw error;
  }

  const signedPayload = `${timestamp}.${rawBody}`;
  const expectedSignature = crypto
    .createHmac('sha256', secret)
    .update(signedPayload)
    .digest('hex');

  if (expectedSignature !== signature) {
    const error = new Error('Invalid PathPulse signature');
    error.statusCode = 401;
    throw error;
  }
}

async function handlePathPulseWebhook({
  body,
  rawBody,
  signature,
  timestamp,
  source,
  ipAddress,
}) {
  verifyPathPulseSignature(rawBody, signature, timestamp);

  const traceId =
    body?.traceId ||
    crypto.randomUUID?.() ||
    new Date().getTime().toString();

  await driverWebhookQueueService.enqueueWebhookEvent({
    eventType: body?.eventType || 'telemetry',
    source: source || 'pathpulse',
    signature,
    timestamp,
    payload: body,
    rawPayloadHash: crypto.createHash('sha256').update(rawBody).digest('hex'),
    ipAddress,
    traceId,
  });
}

module.exports = {
  handleDriverAction,
  handlePathPulseWebhook,
  handleDriverLocationUpdate,
};

