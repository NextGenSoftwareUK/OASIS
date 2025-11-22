const { distanceCalculateMetrix } = require('../utils/distanceCalculation');
const { handleCatchError } = require('../utils/errorCatch');

async function calculateDistanceAmount(req, res) {
  const { sourceCoordinate, destinationCoordinate, amountPerKilo } = req.body;

  try {
    const { duration, distance } = await distanceCalculateMetrix(
      sourceCoordinate,
      destinationCoordinate
    );

    if (!duration || !distance) {
      return handleCatchError('none', res);
    }

    // Distance in miles
    const kiloMeters = distance;

    // Calculate distance amount
    const price = (parseFloat(kiloMeters) * amountPerKilo).toFixed(2);

    res.status(201).send({
      totalKilo: kiloMeters + ' km',
      durationInTraffic: duration,
      price: price,
      amountPerKilo: amountPerKilo,
    });
  } catch (error) {
    return handleCatchError(error, res);
  }
}

module.exports = { calculateDistanceAmount };
