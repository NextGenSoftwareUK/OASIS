const metrics = {
  actions: {
    total: 0,
    bySource: {},
    failures: 0,
  },
  locations: {
    total: 0,
    failures: 0,
  },
  queue: {
    processed: 0,
    failed: 0,
    deadLetter: 0,
    bySource: {},
  },
  lastUpdatedAt: null,
};

function incrementBucket(bucket, key, delta = 1) {
  // eslint-disable-next-line no-param-reassign
  bucket[key] = (bucket[key] || 0) + delta;
}

function recordAction({ source = 'unknown', action = 'unknown', status, latencyMs }) {
  metrics.actions.total += 1;
  incrementBucket(metrics.actions.bySource, source);

  if (status === 'failed') {
    metrics.actions.failures += 1;
  }

  metrics.lastUpdatedAt = new Date();

  if (latencyMs !== undefined) {
    metrics.actions.lastLatencyMs = latencyMs;
  }

  metrics.actions.lastAction = action;
}

function recordLocationUpdate({ status, latencyMs }) {
  metrics.locations.total += 1;

  if (status === 'failed') {
    metrics.locations.failures += 1;
  }

  if (latencyMs !== undefined) {
    metrics.locations.lastLatencyMs = latencyMs;
  }

  metrics.lastUpdatedAt = new Date();
}

function recordQueueProcessed({ source = 'pathpulse', eventType, latencyMs }) {
  metrics.queue.processed += 1;
  const key = `${source}:${eventType}`;
  metrics.queue.bySource[key] = metrics.queue.bySource[key] || { processed: 0, failed: 0, deadLetter: 0 };
  metrics.queue.bySource[key].processed += 1;
  if (latencyMs !== undefined) {
    metrics.queue.lastLatencyMs = latencyMs;
  }
  metrics.lastUpdatedAt = new Date();
}

function recordQueueFailure({ source = 'pathpulse', eventType }) {
  metrics.queue.failed += 1;
  const key = `${source}:${eventType}`;
  metrics.queue.bySource[key] = metrics.queue.bySource[key] || { processed: 0, failed: 0, deadLetter: 0 };
  metrics.queue.bySource[key].failed += 1;
  metrics.lastUpdatedAt = new Date();
}

function recordQueueDeadLetter({ source = 'pathpulse', eventType }) {
  metrics.queue.deadLetter += 1;
  const key = `${source}:${eventType}`;
  metrics.queue.bySource[key] = metrics.queue.bySource[key] || { processed: 0, failed: 0, deadLetter: 0 };
  metrics.queue.bySource[key].deadLetter += 1;
  metrics.lastUpdatedAt = new Date();
}

function getSnapshot() {
  return {
    ...metrics,
    lastUpdatedAt: metrics.lastUpdatedAt,
  };
}

module.exports = {
  recordAction,
  recordLocationUpdate,
  recordQueueProcessed,
  recordQueueFailure,
  recordQueueDeadLetter,
  getSnapshot,
};

