const Driver = require('../models/driverModel');
const Car = require('../models/carModal');

async function getDriverSnapshot(driverId) {
  const driver = await Driver.findById(driverId).select('-password');

  if (!driver) {
    const error = new Error('Driver not found');
    error.statusCode = 404;
    throw error;
  }

  const car = await Car.findOne({ driver: driverId, isActive: true }).lean();

  return {
    driver: driver.toJSON(),
    car,
  };
}

async function updateDriverLocation(driverId, locationPayload) {
  const driver = await Driver.findById(driverId);

  if (!driver) {
    const error = new Error('Driver not found');
    error.statusCode = 404;
    throw error;
  }

  driver.location = {
    latitude: locationPayload.latitude,
    longitude: locationPayload.longitude,
  };

  if (typeof locationPayload.bearing === 'number') {
    driver.heading = locationPayload.bearing;
  }

  if (typeof locationPayload.speed === 'number') {
    driver.speed = locationPayload.speed;
  }

  await driver.save();

  const activeCar = await Car.findOneAndUpdate(
    { driver: driverId, isActive: true },
    {
      $set: {
        location: {
          latitude: locationPayload.latitude,
          longitude: locationPayload.longitude,
        },
      },
    },
    { new: true }
  ).lean();

  return {
    driver: driver.toJSON(),
    car: activeCar,
  };
}

async function updateDriverAvailability(driverId, statusPayload) {
  const driver = await Driver.findById(driverId);

  if (!driver) {
    const error = new Error('Driver not found');
    error.statusCode = 404;
    throw error;
  }

  const carUpdate = {};

  if (typeof statusPayload.isOffline === 'boolean') {
    carUpdate.isOffline = statusPayload.isOffline;
  }

  if (typeof statusPayload.isActive === 'boolean') {
    carUpdate.isActive = statusPayload.isActive;
  }

  if (statusPayload.state) {
    carUpdate.state = statusPayload.state.toLowerCase();
  }

  const activeCar = await Car.findOneAndUpdate(
    { driver: driverId },
    { $set: carUpdate },
    { new: true }
  ).lean();

  return {
    driver: driver.toJSON(),
    car: activeCar,
  };
}

module.exports = {
  getDriverSnapshot,
  updateDriverLocation,
  updateDriverAvailability,
};
