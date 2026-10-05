const Car = require('../models/carModal');
const GlobalSettings = require('../models/globalSettingsModal');
const Otp = require('../models/otpModal');
const { canRide } = require('./bookingUtil');
const { convertTime, convertDate } = require('./dateTimeFormat');
const { getTravelTime } = require('./distanceCalculation');

// --- Main scenerio (incase we add cash payment)
//  This uses the car trip amount to calculate
// if driverWalletPercentage(0.15) from Gloal setting
// Of trip amount is not equivalent to drivers currnt wallet amount
// then driver cant ride , else driver can

// --- Scenerio 2
// Driver with an existing booking on save day of current booking
// Should be able to have finish his pending or on going booking
// an interval of an  hour to the new booking
async function getCarsProximityUtil({
  res,
  sourceLatitude,
  sourceLongitude,
  destinationLatitude,
  destinationLongitude,
  state,
  page,
  pageSize,
  scheduledDate,
} = {}) {
  const query = {
    state: state.toLowerCase(),
    isVerify: true,
    isActive: true,
    isOffline: false,
  };

  const totalCars = await Car.countDocuments(query, { maxTimeMS: 30000 });
  const totalPages = Math.ceil(totalCars / pageSize);

  const cars = await Car.find(query)
    .populate({
      path: 'driver',
      select: 'wallet',
    })
    .skip((page - 1) * pageSize)
    .limit(pageSize)
    .exec();

  // Ride distance from source to destination
  let {
    duration: rideDuration,
    distance: rideDistance,
    distanceNotation: rideDistanceNotation,
  } = await getTravelTime(
    { lat: parseFloat(sourceLatitude), lng: parseFloat(sourceLongitude) },
    {
      lat: parseFloat(destinationLatitude),
      lng: parseFloat(destinationLongitude),
    },
    res
  );

  // trip on same day from otpModal ( it hold accepted Ride)
  const daysTrip = await findMatchingOtps(scheduledDate);

  const adminSettings = await GlobalSettings.findOne();

  // Calculate Ride Amount
  const rideDistanceAmount =
    rideDistance === 0 ? 10 : rideDistance * adminSettings.pricePerKm;

  const carPromises = cars.map(async (car) => {
    // Car distance Away
    let {
      duration: durationAway,
      distance: distanceAway,
      distanceNotation: distanceAwayNotation,
    } = await getTravelTime(
      {
        lat: parseFloat(sourceLatitude),
        lng: parseFloat(sourceLongitude),
      },
      {
        lat: car._doc.location.latitude,
        lng: car._doc.location.longitude,
      },
      res
    );

    // Get trip days for current car driver if present in trip for the day
    const currentDriverDayTrip = daysTrip.filter(
      (trip) => trip.driverId.toString() === car._doc.driver._id.toString()
    );

    // Get driver wallet amount and distance
    // If allowed this driver can Ride

    const sourceLocationForNewTrip = { sourceLatitude, sourceLongitude };

    const isAllowedRide = await canRide(
      car._doc.driver.wallet,
      rideDistanceAmount,
      adminSettings?.driverWalletPercentage,
      rideDuration,
      currentDriverDayTrip,
      scheduledDate,
      sourceLocationForNewTrip,
      res
    );

    if (isAllowedRide === true) {
      const { _id, __v, ...otherData } = car._doc;
      return {
        id: _id,
        ...otherData,

        duration: rideDuration ?? null,
        distance:
          rideDistance === 1
            ? '1 km'
            : rideDistance + ' ' + rideDistanceNotation,
        rideAmount:
          rideDistanceAmount.toFixed(2) < 30
            ? 30
            : rideDistanceAmount.toFixed(2),
        durationAway: durationAway ?? null,
        distanceAway:
          distanceAway === 1
            ? '1 km'
            : distanceAway + ' ' + distanceAwayNotation,
      };
    }
  });

  const sortedCarsWithDistance = (await Promise.all(carPromises)).filter(
    Boolean
  );

  console.log(sortedCarsWithDistance, '  cars promise ');

  sortedCarsWithDistance.sort(
    (a, b) =>
      parseFloat(a.distanceAway.split(' ')[0]) -
      parseFloat(b.distanceAway.split(' ')[0])
  );

  return { cars: sortedCarsWithDistance, totalPages };
}

// Function to find matching Otp documents
// for the scheduled ride departure date

// Function to find matching Otps
// Function to find matching Otps
const findMatchingOtps = async (scheduledDateTime) => {
  // Convert iso '%Y-%m-%dT%H:%m:%s.000Z' Standerd sent from client to '%Y-%m-%d' format
  let scheduledDate;

  try {
    scheduledDate = scheduledDateTime.slice(0, 10);
  } catch (error) {
    console.error('Error processing scheduledDateTime:', error.message);
    console.log('In matchin otp ', scheduledDateTime);
  }
  try {
    const matchingOtps = await Otp.aggregate([
      {
        // Lookup Booking documents referenced by bookingId
        $lookup: {
          from: 'bookings', // The collection name in the database
          localField: 'bookingId',
          foreignField: '_id',
          as: 'bookingDetails',
        },
      },
      {
        // Unwind the bookingDetails array
        $unwind: '$bookingDetails',
      },
      {
        // Extract the date part from booking's departureTime
        $addFields: {
          departureDate: {
            $dateToString: {
              format: '%Y-%m-%d',
              date: '$bookingDetails.departureTime',
            },
          },
        },
      },
      {
        // Convert the scheduledDate to a date string format '%Y-%m-%d' for comparison
        $addFields: {
          formattedScheduledDate: {
            $dateToString: {
              format: '%Y-%m-%d',
              date: {
                $dateFromString: {
                  dateString: scheduledDate,
                  format: '%Y-%m-%d',
                },
              },
            },
          },
        },
      },

      {
        // Match Otps where booking's departureDate equals the formattedScheduledDate and isCancelled is false
        $match: {
          $and: [
            {
              $expr: {
                $eq: ['$departureDate', '$formattedScheduledDate'],
              },
            },
            {
              'cancelledTrip.isCancelled': false,
            },
            {
              'endTrip.isActive': false,
            },
          ],
        },
      },
      {
        // Project the necessary fields (optional)
        $project: {
          _id: 1,
          bookingId: 1,
          driverId: 1,
          'bookingDetails.departureTime': 1,
          'bookingDetails.sourceLocation': 1,
          'bookingDetails.destinationLocation': 1,
          'bookingDetails.tripDistance': 1,
          'bookingDetails.tripDuration': 1,
          departureDate: 1,
          formattedScheduledDate: 1,
          createdAt: 1,
          scheduledDateTime,
        },
      },
    ]);

    return matchingOtps;
  } catch (err) {
    console.error('Error finding matching Otps:', err);
    throw err;
  }
};

module.exports = getCarsProximityUtil;
