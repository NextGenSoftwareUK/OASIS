const { handleCatchError } = require('../utils/errorCatch');
const {
  updateDriverLocation,
  updateDriverAvailability,
  getDriverSnapshot,
} = require('../services/driverService');

function assertDriverScope(req, driverId) {
  if (!req.user) {
    const error = new Error('Unauthorized');
    error.statusCode = 401;
    throw error;
  }

  if (req.user.role === 'admin') {
    return;
  }

  if (req.user.role !== 'driver' || req.user.id.toString() !== driverId) {
    const error = new Error('Unauthorized');
    error.statusCode = 405;
    throw error;
  }
}

async function updateLocation(req, res) {
  const { driverId } = req.params;
  const { latitude, longitude, bearing, speed } = req.body;

  try {
    assertDriverScope(req, driverId);

    if (
      typeof latitude !== 'number' ||
      typeof longitude !== 'number' ||
      Number.isNaN(latitude) ||
      Number.isNaN(longitude)
    ) {
      return res.status(400).json({
        message: 'latitude and longitude are required numeric values',
      });
    }

    const snapshot = await updateDriverLocation(driverId, {
      latitude,
      longitude,
      bearing,
      speed,
    });

    res.json(snapshot);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function updateStatus(req, res) {
  const { driverId } = req.params;
  const { isOffline, isActive, state } = req.body;

  try {
    assertDriverScope(req, driverId);

    if (typeof isOffline !== 'boolean' && typeof isActive !== 'boolean') {
      return res.status(400).json({
        message: 'isOffline or isActive must be provided',
      });
    }

    const snapshot = await updateDriverAvailability(driverId, {
      isOffline,
      isActive,
      state,
    });

    res.json(snapshot);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function getStatus(req, res) {
  const { driverId } = req.params;

  try {
    assertDriverScope(req, driverId);

    const snapshot = await getDriverSnapshot(driverId);
    res.json(snapshot);
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  updateLocation,
  updateStatus,
  getStatus,
};


