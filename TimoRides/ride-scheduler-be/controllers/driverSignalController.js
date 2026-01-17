const crypto = require('crypto');
const { handleCatchError } = require('../utils/errorCatch');
const driverSignalService = require('../services/driverSignalService');

async function handleDriverAction(req, res) {
  try {
    const traceId =
      req.body.traceId ||
      req.headers['x-trace-id'] ||
      crypto.randomUUID?.() ||
      new Date().getTime().toString();

    const result = await driverSignalService.handleDriverAction({
      ...req.body,
      traceId,
    });

    return res.json({
      success: true,
      traceId,
      booking: result.booking,
      driverSnapshot: result.driverSnapshot,
    });
  } catch (error) {
    return handleCatchError(error, res);
  }
}

async function handlePathPulseWebhook(req, res) {
  try {
    const signature = req.get('x-pathpulse-signature') || '';
    const timestamp = req.get('x-pathpulse-timestamp') || '';
    const source = req.get('user-agent') || 'pathpulse';

    await driverSignalService.handlePathPulseWebhook({
      body: req.body,
      rawBody: req.rawBody || JSON.stringify(req.body || {}),
      signature,
      timestamp,
      source,
      ipAddress: req.ip,
    });

    return res.status(202).json({ success: true });
  } catch (error) {
    return handleCatchError(error, res);
  }
}

async function handleDriverLocationUpdate(req, res) {
  try {
    const result = await driverSignalService.handleDriverLocationUpdate(req.body);
    return res.json({
      success: true,
      driverSnapshot: result,
    });
  } catch (error) {
    return handleCatchError(error, res);
  }
}

module.exports = {
  handleDriverAction,
  handlePathPulseWebhook,
  handleDriverLocationUpdate,
};

