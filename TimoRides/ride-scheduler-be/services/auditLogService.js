const AuditLog = require('../models/auditLogModel');
const logger = require('../utils/logger');

async function recordAuditLog({
  booking,
  action,
  actor,
  previous,
  current,
  notes,
  traceId,
  source = 'api',
  metadata = {},
}) {
  if (!booking?.id || !action) {
    return null;
  }

  try {
    const entry = await AuditLog.create({
      booking: {
        id: booking.id,
        status: booking.status,
      },
      action,
      actor,
      previous,
      current,
      notes,
      traceId,
      source,
      metadata,
    });

    logger.debug(
      {
        action,
        traceId,
        bookingId: booking.id,
      },
      'audit log recorded'
    );

    return entry;
  } catch (error) {
    logger.error(
      {
        err: error,
        action,
        bookingId: booking.id,
      },
      'failed to record audit log'
    );
    return null;
  }
}

module.exports = {
  recordAuditLog,
};


