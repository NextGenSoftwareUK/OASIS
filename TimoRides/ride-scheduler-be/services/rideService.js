const Booking = require('../models/bookingModal');
const GlobalSettings = require('../models/globalSettingsModal');

function toNumber(decimalValue) {
  if (!decimalValue) return 0;
  return parseFloat(decimalValue.toString());
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
  return booking;
}

async function startRide(bookingId, driverId) {
  const booking = await fetchBooking(bookingId);
  ensureDriverOwnsBooking(booking, driverId);

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

  const payment = booking.payment || {};
  if (options.paymentStatus) {
    payment.status = options.paymentStatus;
    if (options.paymentStatus === 'paid') {
      payment.paidAt = new Date();
    }
  } else if (!booking.isCash) {
    payment.status = 'paid';
    payment.paidAt = new Date();
  }

  booking.payment = {
    method: payment.method || (booking.isCash ? 'cash' : 'card'),
    status: payment.status || (booking.isCash ? 'pending' : 'paid'),
    reference: payment.reference || null,
    notes: options.paymentNotes || payment.notes || '',
    paidAt: payment.paidAt || null,
  };

  await booking.save();
  return booking;
}

async function recordPayment(bookingId, payload = {}) {
  const booking = await fetchBooking(bookingId);

  booking.payment = {
    method: payload.method || booking.payment?.method || 'cash',
    status: payload.status || booking.payment?.status || 'pending',
    reference: payload.reference || booking.payment?.reference || null,
    notes: payload.notes ?? booking.payment?.notes ?? '',
    paidAt:
      payload.status === 'paid'
        ? new Date()
        : booking.payment?.paidAt || null,
  };

  await booking.save();
  return booking;
}

async function cancelRide(bookingId, actor = {}, reason = '') {
  const booking = await fetchBooking(bookingId);

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

