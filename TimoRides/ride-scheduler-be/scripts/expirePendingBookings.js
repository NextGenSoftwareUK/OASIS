/* eslint-disable no-console */
require('dotenv').config({ path: './config/.env' });
const mongoose = require('mongoose');
const rideService = require('../services/rideService');

async function run() {
  const { Database_Url, BOOKING_EXPIRE_MINUTES } = process.env;

  if (!Database_Url) {
    console.error('Database_Url is missing in config/.env');
    process.exit(1);
  }

  await mongoose.connect(Database_Url);
  console.log('Connected to MongoDB');

  try {
    const threshold =
      parseInt(BOOKING_EXPIRE_MINUTES || '15', 10) || 15;
    const result = await rideService.expirePendingBookings({
      thresholdMinutes: threshold,
    });

    console.log(`Expired ${result.expired} pending bookings (threshold ${threshold} mins).`);
  } catch (error) {
    console.error('Failed to expire bookings', error);
  } finally {
    await mongoose.disconnect();
    console.log('Disconnected');
  }
}

run();

