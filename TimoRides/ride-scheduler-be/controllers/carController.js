require('dotenv').config({ path: '../config/.env' });

const Booking = require('../models/bookingModal');
const Car = require('../models/carModal');
const { getCurrectActiveCar } = require('../utils/carUtil');
const { handleCatchError } = require('../utils/errorCatch');
const { getWallet } = require('../utils/modelGetterUtil');
const getCarsProximityUtil = require('../utils/proximityUtil');

async function getCars(req, res) {
  const { driverId } = req.query;

  try {
    const allCars = !driverId
      ? await Car.find()
      : await Car.find({ driver: driverId });

    res.json(allCars);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function createCar(req, res) {
  // Get payload
  const data = req.body;
  const user = req.user;

  // Check if user is Rider or admin
  if (user.role !== 'driver') {
    return res
      .status(405)
      .json({ message: 'You are not authorized - Must be a Driver' });
  }

  // Stop driver from creating when
  //  car is active and when car is not verified and not active
  // present
  const isDriverCar = await Car.find({
    driver: user.id,
    $or: [{ isActive: true }, { isVerify: false, isActive: false }],
  });

  if (isDriverCar.length !== 0) {
    return res.status(405).json({
      message: 'You can not create a can as you have a car present',
    });
  }

  try {
    const driverId = user._id;

    const car = new Car({
      ...data,
      state: data.state.toLowerCase(),
      driver: driverId,
    });

    const newCar = await car.save();

    res.status(201).json(newCar);
  } catch (error) {
    res.json({ message: error.mesage });
  }
}

// Update Car details uses driverId
async function updateCar(req, res) {
  const { driverId } = req.params;
  const { driver, rating, isVerify, isActive, ...updateData } = req.body;

  const user = req.user;

  // Check if user is Rider or admin
  if (user.role !== 'driver' && user.role !== 'admin') {
    return res
      .status(405)
      .json({ message: 'You are not authorized - Must be a Driver' });
  }

  try {
    // search for car with driver id
    const car = await Car.find({ driver: driverId });

    if (car.length === 0) {
      return res.status(404).json({ message: 'Not found' });
    }

    // For multiple cars owned by driver
    // We only need the currently active or verified car
    // Note driver can have on ecar at a time
    if (car.length > 1) {
      car = car.filter(
        (carData) => carData.isActive === false && carData.isVerify === false
      );
    }

    // If car is verified or active
    //  Driver can not update an already
    // verified active car

    if (car[0].isActive === true && car[0].isVerify === true) {
      return res.status(405).json({
        message: 'Contact customer care service for updating a verified car',
      });
    } else {
      // Update car

      const updatedCar = await Car.findByIdAndUpdate(
        car[0].id,
        updateData,
        { new: true } // Return the updated document
      );

      res.json(updatedCar);
    }
  } catch (error) {
    handleCatchError(error, res);
  }
}

// Get current car for driver
async function getCurrentCar(req, res) {
  const { driverId } = req.params;

  try {
    // search for car with driver id
    let cars = await Car.find({ driver: driverId });

    if (cars.length === 0) {
      return res.status(404).json({ message: 'Not found' });
    }

    // For multiple cars owned by driver
    // We only need the currently active or verified car
    // Note driver can have on ecar at a time
    if (cars.length > 1) {
      const car = getCurrectActiveCar(cars);

      // To return as array of object , as client requires
      return res.json([car[0]]);
    }

    res.json(cars);
  } catch (error) {
    handleCatchError(error, res);
  }
}

// Get car by Id and optional Booking id is ride is rebooking inother to show ride amount
async function getCar(req, res) {
  const { carId } = req.params;

  const { bookingId } = req.query;

  let rideAmount = null;
  let durationAway = null;
  let distanceAway = null;
  let duration = null;
  let distance = null;

  try {
    if (bookingId) {
      const booking = await Booking.findOne({ _id: bookingId });

      if (!booking) {
        return res
          .status(405)
          .json({ message: 'Invalid booking id, Booking not found' });
      }

      rideAmount = getWallet(booking.tripAmount);
      duration = booking?.duration;
      distance = booking?.distance;
      durationAway = booking?.durationAway;
      distanceAway = booking?.distanceAway;
    }

    const car = await Car.findOne({ _id: carId })
      .populate({
        path: 'driver',
      })
      .exec();

    if (!car) {
      return res.status(404).json({ message: 'Car not found' });
    }

    const { _id, __v, ...otherCarDetails } = car._doc;

    const carRes = {
      id: _id,
      ...otherCarDetails,
      rideAmount,
      durationAway,
      distanceAway,
      duration,
      distance,
    };

    res.json(carRes);
  } catch (error) {
    handleCatchError(error, res);
  }
}

// Promximity: 3km away from user
async function getCarsProximity(req, res) {
  // Pagination parameters
  const page = req.query.page ? parseInt(req.query.page) : 1;
  const pageSize = req.query.pageSize ? parseInt(req.query.pageSize) : 10;

  const {
    sourceLatitude,
    sourceLongitude,
    destinationLatitude,
    destinationLongitude,
    state,
    scheduledDate,
  } = req.query; //  { latitude: 123, longitude: 456 }

  try {
    const { cars, totalPages } = await getCarsProximityUtil({
      res,
      sourceLatitude,
      sourceLongitude,
      destinationLatitude,
      destinationLongitude,
      state,
      page,
      pageSize,
      scheduledDate,
    });
    // Return paginated results
    return res.json({
      cars: cars,
      currentPage: page,
      totalPages: totalPages,
    });
  } catch (error) {
    return null;
  }
}

module.exports = {
  getCars,
  createCar,
  updateCar,
  getCar,
  getCurrentCar,
  getCarsProximity,
};
