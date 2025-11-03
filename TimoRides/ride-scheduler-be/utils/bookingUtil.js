require('dotenv').config({ path: '../config/.env' });
const moment = require('moment');
const { getTravelTime } = require('./distanceCalculation');

async function canRide(
  walletBalance,
  rideDistanceAmount,
  driverPercent,
  rideDuration,
  currentDriverDayTrip,
  scheduledDate,
  sourceLocationForNewTrip,
  res
) {
  let isNewBookingAllowed =
    currentDriverDayTrip.length > 0
      ? await canDriverAcceptNewBooking(
          currentDriverDayTrip,
          scheduledDate,
          rideDuration,
          sourceLocationForNewTrip,
          res
        )
      : true;

  // Calculate
  const amountPercentage = rideDistanceAmount * driverPercent;

  const isAllowedRide = walletBalance >= amountPercentage ? true : false;

  // trip on same day

  return isAllowedRide && isNewBookingAllowed;
}

module.exports = {
  canRide,
};

// Utils

// Function to parse the estimated time duration
function parseDuration(duration) {
  const timeUnits = duration.split(' ');
  let totalMinutes = 0;

  for (let i = 0; i < timeUnits.length; i += 2) {
    const value = parseInt(timeUnits[i]);
    const unit = timeUnits[i + 1].toLowerCase();

    if (unit.includes('hour') || unit.includes('hours')) {
      totalMinutes += value * 60;
    } else if (unit.includes('min') || unit.includes('mins')) {
      totalMinutes += value;
    } else if (unit.includes('day') || unit.includes('days')) {
      totalMinutes += value * 24 * 60;
    }
  }

  return totalMinutes;
}

// Function to check if two time periods overlap
const isOverlapping = (start1, end1, start2, end2) => {
  return moment(start1).isBefore(end2) && moment(start2).isBefore(end1);
};

async function calculateBufferTime(
  curExistingRideDestLocation,
  newRideSourceLocation,
  res
) {
  const { sourceLatitude, sourceLongitude } = newRideSourceLocation;

  const { latitude: destinationLatitude, longitude: destinationLongitude } =
    curExistingRideDestLocation;

  const { duration: travelDuration } = await getTravelTime(
    { lat: parseFloat(sourceLatitude), lng: parseFloat(sourceLongitude) },
    {
      lat: parseFloat(destinationLatitude),
      lng: parseFloat(destinationLongitude),
    },
    res
  );

  // Parse the duration and add half of it as extra buffer time
  const travelDurationMinutes = parseDuration(travelDuration);
  const extraBufferMinutes = travelDurationMinutes + travelDurationMinutes / 2;

  return extraBufferMinutes;
}

// Function to check if a driver can accept a new booking
async function canDriverAcceptNewBooking(
  existingBookings,
  newRideStart,
  newRideDuration,
  newRideSourceLocation,
  res
) {
  const newRideDurationMinutes = parseDuration(newRideDuration);
  const newRideEnd = moment(newRideStart).add(
    newRideDurationMinutes,
    'minutes'
  );

  for (const booking of existingBookings) {
    const currentRideStart = booking.bookingDetails.departureTime;
    const currentRideDuration = parseDuration(
      booking.bookingDetails.tripDuration
    );
    const currentRideEnd = moment(currentRideStart).add(
      currentRideDuration,
      'minutes'
    );

    if (
      isOverlapping(currentRideStart, currentRideEnd, newRideStart, newRideEnd)
    ) {
      return false; // Overlapping found, driver cannot accept the new booking
    } else {
      // Now call a function that checks the other existing rides in the array
      // if it overlaps with the current existing ride in the array

      // Calculate buffer time for current ride
      // (get distance from source location of newRide
      // to Destination Location of current Exsiting ride)
      const currentRideBuffer = await calculateBufferTime(
        booking.bookingDetails.destinationLocation,
        newRideSourceLocation,
        res
      );

      const currentRideEndWithBuffer = moment(currentRideEnd).add(
        currentRideBuffer,
        'minutes'
      );

      for (const otherBooking of existingBookings) {
        if (otherBooking.bookingId !== booking.bookingId) {
          const otherRideStart = otherBooking.bookingDetails.departureTime;
          const otherRideDuration = parseDuration(
            otherBooking.bookingDetails.tripDuration
          );
          const otherRideEnd = moment(otherRideStart).add(
            otherRideDuration,
            'minutes'
          );

          if (
            isOverlapping(
              currentRideStart,
              currentRideEndWithBuffer,
              otherRideStart,
              otherRideEnd
            )
          ) {
            return false; // Overlapping found among existing rides
          }
        }
      }
    }
  }

  return true; // No overlap found, driver can accept the new booking
}
