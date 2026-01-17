const Otp = require('../models/otpModal');
const { handleCatchError } = require('../utils/errorCatch');
const User = require('../models/userModel');
const Booking = require('../models/bookingModal');
const rideService = require('../services/rideService');

async function getAllTripDetail(req, res) {
  const { email } = req.query;

  const user = req.user;

  if (user.role !== 'admin') {
    return res
      .status(405)
      .json({ message: ' Unauthorized - seek admin access' });
  }

  try {
    if (email) {
      // Find the user by email
      const userData = await User.findOne({ email: email });

      if (!userData) {
        return res.status(404).json({ message: 'User not found' });
      }

      if (userData.role !== 'driver') {
        return res.status(404).json({ message: 'User not a driver' });
      }

      const otpData = await Otp.find({ driverId: userData.id });

      return res.json(otpData);
    }

    const otpData = await Otp.find();

    res.json(otpData);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function confirmOtp(req, res) {
  // Get BookingId
  const { bookingId, otpCode, tripMode } = req.body;

  const user = req.user;

  // Validation of Trip mode
  try {
    const otpTrip = await Otp.findOne({ bookingId: bookingId });

    if (!otpTrip) {
      return res.status(404).json({ message: 'OTP not found' });
    }

    const booking = await Booking.findById(bookingId);

    // Check if user is authorized
    // as the main driver of the trip

    if (otpTrip.driverId.toString() !== user.id) {
      return res
        .status(405)
        .json({ message: 'unauthorized you are not the driver' });
    }

    // Stop from updating end trip when start trip is not update true
    if (tripMode === 'end' && otpTrip.startTrip.isActive === false) {
      return res
        .status(405)
        .json({ message: ' You need to first confirm start trip' });
    }

    // Determine the code to compare based on the tripMode
    const codeToCompare =
      tripMode.trim() === 'start'
        ? otpTrip.startTrip
        : tripMode.trim() === 'end'
        ? otpTrip.endTrip
        : null;

    if (codeToCompare === null) {
      return res.status(405).json({ message: 'Trip Value not indicated' });
    }

    // Check if code has being actived already
    if (codeToCompare.isActive === true) {
      return res.json({ message: ' Code has already being used' });
    }

    // Compare Code
    if (codeToCompare.code !== otpCode) {
      return res.json({ message: ' Code is invalid' });
    }

    // Update driver completed trip if end trip
    if (tripMode.trim() === 'end') {
      await rideService.completeRide(bookingId, user.id);
    }

    if (tripMode.trim() === 'start') {
      await rideService.startRide(bookingId, user.id);
    }

    // Update state to is actived
    // Set Date Time of Activation

    // Update the trip code's state to active and set the activation time
    const updateFields = {
      [`${tripMode}Trip.codeDate`]: new Date(),
      [`${tripMode}Trip.isActive`]: true,
    };

    const updatedOtp = await Otp.findByIdAndUpdate(
      otpTrip._id,
      { $set: updateFields },
      {
        new: true,
      }
    );

    // For securing response info
    // start trip shows only when its used and same
    const { endTrip, ...secureStart } = updatedOtp._doc;
    const jsonUpdated = tripMode === 'start' ? secureStart : updatedOtp;

    res.json(jsonUpdated);
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  getAllTripDetail,
  confirmOtp,
};
