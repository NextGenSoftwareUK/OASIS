// Helper function to calculate distance between two coordinates using Haversine formula
require('dotenv').config({ path: '../config/.env' });
const axios = require('axios');
const { handleCatchError } = require('./errorCatch');
const { removeCommaFromNumber } = require('./dateTimeFormat');

function getDistance(lat1, lon1, lat2, lon2) {
  const R = 6371; // Radius of Earth in kilometers
  const dLat = deg2rad(lat2 - lat1);
  const dLon = deg2rad(lon2 - lon1);
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(deg2rad(lat1)) *
      Math.cos(deg2rad(lat2)) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  const distance = R * c; // Distance in kilometers
  return distance;
}

function deg2rad(deg) {
  return deg * (Math.PI / 180);
}

// Calculate Metrix
async function distanceCalculateMetrix(
  sourceCoordinate,
  destinationCoordinate
) {
  try {
    const response = await axios.get(
      `https://api.distancematrix.ai/maps/api/distancematrix/json?origins=${sourceCoordinate?.lat},${sourceCoordinate?.lng}&destinations=${destinationCoordinate?.lat},${destinationCoordinate?.lng}&mode=driving&departure_time=now&key=${process.env.DISTANCE_METRIX}`
    );

    const stringData = JSON.stringify(response.data);
    const data = JSON.parse(stringData);

    const finalData = {
      duration: data?.rows[0]?.elements[0].duration_in_traffic?.text,
      distance: data?.rows[0]?.elements[0].distance?.text
        ? data?.rows[0]?.elements[0].distance?.text.split(' ')[0]
        : undefined,
    };

    return finalData;
  } catch (error) {
    console.error(error);
  }
}

// For Goole Clound
async function getTravelTime(origin, destination, res) {
  const url = `https://maps.googleapis.com/maps/api/distancematrix/json?units=metric&origins=${origin.lat},${origin.lng}&destinations=${destination.lat},${destination.lng}&key=${process.env.GOOGLE_CLOUD}`;

  try {
    const response = await axios.get(url);
    const data = response.data;

    if (data.rows.length > 0 && data.rows[0].elements.length > 0) {
      const element = data.rows[0].elements[0];

      let distance =
        element?.distance?.text.split(' ')[1] === 'm'
          ? '1'
          : element?.distance?.text.split(' ')[0]; // e.g., "394 km"

      const distanceNotation = element?.distance?.text.split(' ')[1];

      const duration = element.duration.text; // e.g., "4 hours 37 mins"

      distance = removeCommaFromNumber(distance);

      return { distance, duration, distanceNotation };
    } else {
      return res
        .status(400)
        .json({ message: 'No data available for the given coordinates' });
    }
  } catch (error) {
    return res.status(400).json({
      message:
        'Invalid coordinates provided. Please ensure the coordinates are correct.',
    });
  }
}

module.exports = { getDistance, distanceCalculateMetrix, getTravelTime };
