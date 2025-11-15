const Booking = require('../models/bookingModal');
const GlobalSettings = require('../models/globalSettingsModal');
const { recordAuditLog } = require('./auditLogService');

const PAYMENT_METHODS = ['cash', 'wallet', 'card', 'mobile_money'];
const PAYMENT_TRANSITIONS = {
  unpaid: ['pending', 'paid', 'refunded'],
  pending: ['paid', 'refunded'],
  paid: ['refunded'],
  refunded: [],
};

function toNumber(decimalValue) {
  if (!decimalValue) return 0;
  return parseFloat(decimalValue.toString());
}

function snapshotBookingState(booking) {
  if (!booking) return null;
  return JSON.parse(
    JSON.stringify({
      status: booking.status,
      payment: booking.payment,
      timeline: booking.timeline,
      cancellation: booking.cancellation,
    })
  );
}

function resolveActor(metaActor, booking) {
  if (metaActor) {
    return metaActor;
  }

  const driver = booking?.car?.driver;

  if (driver) {
    return {
      id: driver.id,
      role: 'driver',
      fullName: driver.fullName,
      email: driver.email,
    };
  }

  return null;
}

function ensureBookingIsActive(booking) {
  if (booking.status === 'cancelled') {
    const error = new Error('Cancelled bookings cannot be updated');
    error.statusCode = 409;
    throw error;
  }
}

function ensureValidPaymentTransition(currentStatus = 'unpaid', nextStatus) {
  if (currentStatus === nextStatus || !nextStatus) {
    return;
  }

  const allowed = PAYMENT_TRANSITIONS[currentStatus] || [];

  if (!allowed.includes(nextStatus)) {
    const error = new Error(
      `Payment status cannot transition from ${currentStatus} to ${nextStatus}`
    );
    error.statusCode = 409;
    throw error;
  }
}

function applyPaymentUpdate(booking, payload = {}) {
  const existing = booking.payment || {};
  const currentStatus = existing.status || 'unpaid';
  const nextStatus = payload.status || currentStatus;

  ensureValidPaymentTransition(currentStatus, nextStatus);

  const method =
    payload.method ||
    existing.method ||
    (booking.isCash ? 'cash' : 'card');

  if (!PAYMENT_METHODS.includes(method)) {
    const error = new Error(`Unsupported payment method: ${method}`);
    error.statusCode = 400;
    throw error;
  }

  const updated = {
    method,
    status: nextStatus,
    reference: payload.reference ?? existing.reference ?? null,
    notes: payload.notes ?? existing.notes ?? '',
    paidAt: existing.paidAt || null,
  };

  if (currentStatus !== 'paid' && nextStatus === 'paid') {
    updated.paidAt = payload.paidAt || new Date();
  } else if (nextStatus !== 'paid') {
    updated.paidAt = payload.paidAt || null;
  }

  return updated;
}

async function fetchBooking(bookingId) {
  const booking = await Booking.findById(bookingId)
    .populate({
      path: 'car',
      populate: {
        path: 'driver',
      },
    })
    .populate({
      path: 'user',
    })
    .exec();

  if (!booking) {
    const error = new Error('Booking not found');
    error.statusCode = 404;
    throw error;
  }

  return booking;
}

function ensureDriverOwnsBooking(booking, driverId) {
  if (!booking?.car?.driver) {
    const error = new Error('Booking driver not found');
    error.statusCode = 404;
    throw error;
  }

  if (booking.car.driver.id.toString() !== driverId.toString()) {
    const error = new Error('Unauthorized - booking assigned to another driver');
    error.statusCode = 405;
    throw error;
  }
}

async function acceptBooking(bookingId, driverId, meta = {}) {
  const booking = await fetchBooking(bookingId);
  ensureDriverOwnsBooking(booking, driverId);
  ensureBookingIsActive(booking);
  const previous = snapshotBookingState(booking);

  if (booking.status !== 'pending') {
    const error = new Error('Ride acceptance status already updated');
    error.statusCode = 409;
    throw error;
  }

  booking.status = 'accepted';
  booking.isDriverAccept = true;
  booking.timeline = {
    ...(booking.timeline || {}),
    acceptedAt: new Date(),
  };
  booking.trxId = meta.trxId || booking.trxId;

  await booking.save();
  await recordAuditLog({
    booking: { id: booking.id, status: booking.status },
    action: 'booking.accepted',
    actor: resolveActor(meta.actor, booking),
    previous,
    current: snapshotBookingState(booking),
    traceId: meta.traceId,
    metadata: { source: meta.source || 'driver', trxId: booking.trxId },
  });
  return booking;
}

async function startRide(bookingId, driverId) {
  const booking = await fetchBooking(bookingId);
  ensureDriverOwnsBooking(booking, driverId);
  ensureBookingIsActive(booking);
  const previous = snapshotBookingState(booking);

  if (booking.status !== 'accepted') {
    const error = new Error('Ride must be accepted before starting');
    error.statusCode = 409;
    throw error;
  }

  booking.status = 'started';
  booking.timeline = {
    ...(booking.timeline || {}),
    startedAt: new Date(),
  };

  await booking.save();
  await recordAuditLog({
    booking: { id: booking.id, status: booking.status },
    action: 'booking.started',
    actor: resolveActor(null, booking),
    previous,
    current: snapshotBookingState(booking),
  });
  return booking;
}

async function creditDriverWallet(booking) {
  const driver = booking.car.driver;

  const tripAmount = toNumber(booking.tripAmount);

  const settings = await GlobalSettings.findOne();
  const businessCommission = settings ? toNumber(settings.businessCommission) : 0;

  const companyShare = tripAmount * businessCommission;
  const driverPayout = tripAmount - companyShare;

  const currentWallet = toNumber(driver.wallet);

  driver.wallet = currentWallet + driverPayout;
  driver.completedRides = (driver.completedRides || 0) + 1;

  await driver.save();
}

async function completeRide(bookingId, driverId, options = {}) {
  const booking = await fetchBooking(bookingId);
  ensureDriverOwnsBooking(booking, driverId);
  ensureBookingIsActive(booking);
  const previous = snapshotBookingState(booking);

  if (booking.status === 'completed') {
    return booking;
  }

  if (booking.status !== 'started') {
    const error = new Error('Ride must be started before completion');
    error.statusCode = 409;
    throw error;
  }

  await creditDriverWallet(booking);

  booking.status = 'completed';
  booking.timeline = {
    ...(booking.timeline || {}),
    completedAt: new Date(),
  };

  const paymentUpdate =
    options.paymentStatus ||
    (!booking.isCash ? 'paid' : null);

  booking.payment = applyPaymentUpdate(booking, {
    status: paymentUpdate ? paymentUpdate : undefined,
    notes: options.paymentNotes,
  });

  await booking.save();
  await recordAuditLog({
    booking: { id: booking.id, status: booking.status },
    action: 'booking.completed',
    actor: resolveActor(options.actor, booking),
    previous,
    current: snapshotBookingState(booking),
    traceId: options.traceId,
    metadata: { paymentStatus: booking.payment?.status },
  });
  return booking;
}

async function recordPayment(bookingId, payload = {}, options = {}) {
  const booking = await fetchBooking(bookingId);
  ensureBookingIsActive(booking);
  const previous = snapshotBookingState(booking);

  booking.payment = applyPaymentUpdate(booking, payload);

  await booking.save();
  await recordAuditLog({
    booking: { id: booking.id, status: booking.status },
    action: 'booking.payment.updated',
    actor: resolveActor(options.actor, booking),
    previous,
    current: snapshotBookingState(booking),
    traceId: options.traceId,
    notes: payload.notes,
    metadata: { reference: booking.payment?.reference },
  });
  return booking;
}

async function cancelRide(bookingId, actor = {}, reason = '') {
  const booking = await fetchBooking(bookingId);
  const previous = snapshotBookingState(booking);

  if (booking.status === 'cancelled') {
    return booking;
  }

  booking.status = 'cancelled';
  booking.timeline = {
    ...(booking.timeline || {}),
    cancelledAt: new Date(),
  };

  booking.cancellation = {
    reason,
    cancelledBy: actor?.id
      ? {
          id: actor.id,
          role: actor.role,
          fullName: actor.fullName,
        }
      : booking.cancellation?.cancelledBy,
  };

  await booking.save();
  await recordAuditLog({
    booking: { id: booking.id, status: booking.status },
    action: 'booking.cancelled',
    actor: actor?.id
      ? {
          id: actor.id,
          role: actor.role,
          fullName: actor.fullName,
          email: actor.email,
        }
      : resolveActor(null, booking),
    previous,
    current: snapshotBookingState(booking),
    notes: reason,
  });
  return booking;
}

async function expirePendingBookings({ thresholdMinutes = 15 } = {}) {
  const cutoff = new Date(Date.now() - thresholdMinutes * 60 * 1000);

  const bookings = await Booking.find({
    status: 'pending',
    createdAt: { $lt: cutoff },
  }).exec();

  for (const booking of bookings) {
    booking.status = 'cancelled';
    booking.timeline = {
      ...(booking.timeline || {}),
      cancelledAt: new Date(),
    };
    booking.cancellation = booking.cancellation || {};
    booking.cancellation.reason =
      booking.cancellation.reason || 'auto-expired';
    await booking.save();
  }

  return {
    expired: bookings.length,
  };
}

module.exports = {
  acceptBooking,
  startRide,
  completeRide,
  recordPayment,
  cancelRide,
  expirePendingBookings,
  fetchBooking,
};

