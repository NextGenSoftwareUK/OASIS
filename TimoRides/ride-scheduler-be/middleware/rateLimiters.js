const rateLimit = require('express-rate-limit');

function buildLimiter({ windowMs, max, message }) {
  return rateLimit({
    windowMs,
    max,
    standardHeaders: true,
    legacyHeaders: false,
    message: message || 'Too many requests, please try again later.',
  });
}

const authLimiter = buildLimiter({
  windowMs: Number(process.env.RATE_LIMIT_AUTH_WINDOW_MS) || 15 * 60 * 1000,
  max: Number(process.env.RATE_LIMIT_AUTH_MAX) || 40,
  message: 'Too many auth attempts, slow down.',
});

const bookingLimiter = buildLimiter({
  windowMs: Number(process.env.RATE_LIMIT_BOOKINGS_WINDOW_MS) || 10 * 60 * 1000,
  max: Number(process.env.RATE_LIMIT_BOOKINGS_MAX) || 120,
});

const paymentLimiter = buildLimiter({
  windowMs: Number(process.env.RATE_LIMIT_PAYMENTS_WINDOW_MS) || 10 * 60 * 1000,
  max: Number(process.env.RATE_LIMIT_PAYMENTS_MAX) || 60,
  message: 'Too many payment updates, please retry later.',
});

const driverSignalLimiter = buildLimiter({
  windowMs:
    Number(process.env.RATE_LIMIT_DRIVER_ACTION_WINDOW_MS) || 5 * 60 * 1000,
  max: Number(process.env.RATE_LIMIT_DRIVER_ACTION_MAX) || 200,
});

const webhookLimiter = buildLimiter({
  windowMs:
    Number(process.env.RATE_LIMIT_WEBHOOK_WINDOW_MS) || 60 * 1000,
  max: Number(process.env.RATE_LIMIT_WEBHOOK_MAX) || 600,
});

module.exports = {
  authLimiter,
  bookingLimiter,
  paymentLimiter,
  driverSignalLimiter,
  webhookLimiter,
};

