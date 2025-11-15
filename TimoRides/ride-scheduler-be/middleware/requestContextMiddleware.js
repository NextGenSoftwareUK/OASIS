const crypto = require('crypto');
const logger = require('../utils/logger');

function requestContextMiddleware(req, res, next) {
  const incomingTraceId =
    req.get('x-trace-id') ||
    req.headers['x-trace-id'] ||
    req.headers['x-request-id'];

  const traceId = incomingTraceId || crypto.randomUUID?.() || Date.now().toString();

  req.traceId = traceId;
  res.set('x-trace-id', traceId);

  const startTime = Date.now();
  req.logger = logger.child({
    traceId,
    method: req.method,
    path: req.path,
  });

  res.on('finish', () => {
    const durationMs = Date.now() - startTime;
    req.logger.info(
      {
        statusCode: res.statusCode,
        durationMs,
      },
      'request completed'
    );
  });

  next();
}

module.exports = requestContextMiddleware;

