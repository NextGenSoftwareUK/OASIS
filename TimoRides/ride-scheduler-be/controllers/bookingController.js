const Booking = require('../models/bookingModal');
require('dotenv').config({ path: '../config/.env' });
const Car = require('../models/carModal');
const { convertDate, convertTime } = require('../utils/dateTimeFormat');
const { handleCatchError } = require('../utils/errorCatch');
const generateIdToken = require('../utils/genarateIdToken');
const appRoot = require('app-root-path');
const sendConfirmationEmail = require('../utils/sendEmailUtil');
const path = require('path');
const fs = require('fs');
const jwt = require('jsonwebtoken');
const ejs = require('ejs');
const generateOtp = require('../utils/otpGenerateUtil');
const Otp = require('../models/otpModal');
const User = require('../models/userModel');
const getCarsProximityUtil = require('../utils/proximityUtil');
const shouldPenalizeUser = require('../utils/shouldPenalize');
const rideService = require('../services/rideService');

async function getAllBooking(req, res) {
  const user = req.user;

  let bookings;

  // Search by user id if user in booking is not null

  try {
    // For user
    if (user.role !== 'admin' && user.role === 'user') {
      // Incase user created bookings without a user ref on the booking

      // To get booking with the user id
      const bookingsID = await Booking.find({ user: user.id })
        .populate({
          path: 'car',
          populate: {
            path: 'driver',
          },
        })
        .exec();

      // Get bookings with the user email
      const bookingsEmail = await Booking.find({ email: user.email })
        .populate({
          path: 'car',
          populate: {
            path: 'driver',
          },
        })
        .exec();

      bookings = [...bookingsID, ...bookingsEmail];
      return res.json({ bookings });
    }

    // For driver
    if (user.role !== 'admin' && user.role === 'driver') {
      // Step 1: Find cars with the specified driver
      const cars = await Car.find({ driver: user.id }).select('_id');
      const carIds = cars.map((car) => car.id);

      bookings = await Booking.find({ car: { $in: carIds } })
        .populate({
          path: 'car',
          populate: {
            path: 'driver',
          },
        })
        .exec();
      return res.json({ bookings });
    }

    // For Admin, get all
    bookings = await Booking.find()
      .populate({
        path: 'car',
        populate: {
          path: 'driver',
        },
      })
      .exec();

    res.json({ bookings });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function getBooking(req, res) {
  try {
    const { id } = req.params;

    const booking = await Booking.findOne({ _id: id })
      .populate({
        path: 'car',
        populate: {
          path: 'driver',
        },
      })
      .exec();

    return res.json({ booking });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function createBooking(req, res) {
  try {
    const { status, ...bookingData } = req.body;

    const user = req.user;

    await createBookingUtil(res, bookingData, user);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function reCreateBooking(req, res) {
  const { car: newCar, bookingId } = req.body;

  const user = req.user;

  const booking = await Booking.findById(bookingId);

  // Check if booking exist
  if (!booking) {
    return res.status(404).json({ messaage: 'Cancelled booking id not found' });
  }

  // Remove car and id from old booking
  // To get other booking details
  const { car, _id, status, ...otherBookingData } = booking._doc;

  // If old booking car is same as new car id
  if (car.toString() === newCar) {
    return res.status(405).json({
      messaage: 'You can not re-book same car that initial cancelled booking',
    });
  }

  // Check if booking status is cancelled
  if (status !== 'cancelled') {
    return res
      .status(405)
      .json({ messaage: ' Unauthorized:  booking is not cancelled' });
  }

  // Add new car to booking details
  const bookingData = { car: newCar, ...otherBookingData };

  // Call Create booking
  await createBookingUtil(res, bookingData, user);
}

async function verifyAcceptance(req, res) {
  const { token, isAccepted } = req.body;

  const user = req.user;

  //  If accepted True
  //  Send user mail Ride booked successfully (include user phone number and driver phone number)
  // Send driver mail Ride book sucessfully
  // Change isAccepted in db to true

  try {
    // Decode token
    const { bookingId } = jwt.verify(
      token,
      process.env.VALIDATION_TOKEN_SECRET
    );

    bookingStatus(bookingId, isAccepted, user, res);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function acceptBooking(req, res) {
  const {
    bookingId,

    isAccepted,
  } = req.body;
  const user = req.user;

  try {
    bookingStatus(bookingId, isAccepted, user, res);
  } catch (error) {
    handleCatchError(error, res);
  }
}

// Booking canellation for user/driver cancellation
// Only users with account can cancel
async function cancelBooking(req, res) {
  // Get booking ID

  const { bookingId } = req.body;

  // Get user or Driver initialing cancellation
  const user = req.user;

  let bookingUserAccount = null;

  try {
    // Get booking by Id populate driver and user
    const booking = await Booking.findOne({ _id: bookingId })
      .populate({
        path: 'user',
      })
      .populate({
        path: 'car',
        populate: {
          path: 'driver',
        },
      })
      .exec();

    if (booking.status === 'cancelled') {
      return res.json({ message: 'Booking has previously being cancelled' });
    }
    // Check if logged in user who booked null
    if (booking.user) {
      bookingUserAccount = await User.findById(booking.user);
    }

    // Check if booking exist
    if (!booking || booking.length === 0) {
      // Return not found if not exist
      return res.status(404).json({ message: 'Not found' });
    }

    // Check if user id or Driver id associate with booking
    if (user.id !== booking.car.driver.id && user.id !== booking.user.id) {
      // Return unathorized
      return res.status(405).json({ message: 'Unathorized' });
    }

    await rideService.cancelRide(
      bookingId,
      user,
      user.role === 'user'
        ? 'Cancelled by rider'
        : user.role === 'driver'
        ? 'Cancelled by driver'
        : 'Cancelled'
    );

    // Pernalize user , check if role is user
    let penalize = false;

    if (user.role === 'user') {
      const cancellationDate = Date.now();

      penalize = shouldPenalizeUser(
        booking.createdAt,
        booking.departureTime,
        cancellationDate
      );
    }

    // Save cancellation Details to otp cancellation
    const updateFieldOtpTrip = {
      'cancelledTrip.cancelledDate': new Date(),
      'cancelledTrip.cancelledBy.id': user.id,
      'cancelledTrip.cancelledBy.role': user.role,
      'cancelledTrip.isCancelled': true,
      isPenalized: penalize,
    };

    // Set details of cancellation and update
    await Otp.findOneAndUpdate(
      { bookingId: bookingId },
      { $set: updateFieldOtpTrip },
      {
        new: true,
        upsert: true,
      }
    );

    // Send User email to rebook if Ride cancelled by Driver
    if (user.role === 'driver') {
      // Call driver cancellation
      await driverCancellation(res, bookingUserAccount, booking);
    }

    // Send Driver a email if Ride cancelled by User
    if (user.role === 'user') {
      const bookingCancelledMsg = {
        to: booking.car.driver.email,
        from: process.env.SENDER_MAIL, // Replace with your sender email
        subject: 'Booking cancellation',
        html: `
        <div>
        <h3>Hi ${booking.car.driver.fullName},</h3>

        <p> Booking has being cancelled. by ${booking.fullName} </p>
        </div>
`,
      };

      // Send User a email of cancellation (to offender)
      await sendConfirmationEmail(bookingCancelledMsg);
    }

    // Create message for the canceller,
    // to tell them ride cancellation was successfull
    const bookingCancellerMsg = {
      to: user.email,
      from: process.env.SENDER_MAIL,
      subject: 'Booking cancellation',
      html: `
        <div>
        <h3>
        Hi, ${user.fullName}
        </h3>

        <p>Booking has being cancelled</p>
        </div>
  `,
    };

    // Send Canceller a mail of cancellation success
    await sendConfirmationEmail(bookingCancellerMsg);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function updatePayment(req, res) {
  const { id } = req.params;
  const user = req.user;
  const { method, status, reference, notes } = req.body;

  try {
    const booking = await rideService.fetchBooking(id);

    if (
      user.role !== 'admin' &&
      booking.car?.driver?.id.toString() !== user.id.toString()
    ) {
      return res.status(405).json({ message: 'Unauthorized' });
    }

    const updatedBooking = await rideService.recordPayment(
      id,
      {
        method,
        status,
        reference,
        notes,
      },
      {
        actor: {
          id: user.id,
          role: user.role,
          fullName: user.fullName,
          email: user.email,
        },
        traceId: req.traceId,
        source: 'admin_api',
      }
    );

    res.json({ booking: updatedBooking });
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  getAllBooking,
  createBooking,
  reCreateBooking,
  verifyAcceptance,
  getBooking,
  acceptBooking,
  cancelBooking,
  updatePayment,
};

// Utility For Handling Booking acceptance or cancellation

async function bookingStatus(bookingId, isAccepted, user, res) {
  let bookingUserAccount = null;

  const booking = await Booking.findOne({ _id: bookingId })
    .populate({
      path: 'car',
      populate: {
        path: 'driver',
      },
    })
    .exec();

  // Check if logged in user who booked null
  if (booking.user) {
    bookingUserAccount = await User.findById(booking.user);
  }

  // If auth user is not a driver assocaited to the booking
  if (booking.car.driver.id !== user?.id) {
    return res.status(405).json({ message: 'Unauthorized' });
  }

  if (booking.status !== 'pending') {
    return res
      .status(405)
      .json({ message: 'Ride acceptance Status has already been changed' });
  }

  if (isAccepted === true) {
    // const driver = await Driver.findOne({ _id: car.driver });

    // If Accept Generate start and End Otp trip
    const { startOtpCode, endOtpCode } = generateOtp();

    // Save otp details to Otp schema
    const otpData = {
      bookingId: bookingId,
      driverId: user?.id,
      startTrip: {
        code: startOtpCode,
      },
      endTrip: {
        code: endOtpCode,
      },
      cancelledTrip: { isCancelled: false, cancelledBy: {} },
    };

    // Save otp details to db
    const otpSaved = new Otp(otpData);

    await otpSaved.save();

    // Add otp to msg
    const data = {
      name: user.fullName,
      driverPhone: user.phone,
      userName: booking.fullName,
      userPhone: booking.phoneNumber,
      sourceAddress: booking.sourceLocation.address,
      destinationAddress: booking.destinationLocation.address,
      isPaid: booking.isCash ? 'Pending payment' : 'Paid',
      paymentType: booking.isCash ? 'Cash' : 'Online (paid)',
      amountPaid: booking.tripAmount,
      departureDate: convertDate(booking.departureTime),
      departureTime: convertTime(booking.departureTime),
      trxId: booking.trxId,
    };

    const userExtraEmailData = {
      startTripCode: startOtpCode,
      endTripCode: endOtpCode,
    };

    // Check if the template file exists
    const templatePath = path.join(
      appRoot.path,
      'views',
      'booking-reciept.ejs'
    );

    if (!fs.existsSync(templatePath)) {
      return res.status(404).json({ error: 'Template not found' });
    }

    // User Render the template with the provided data
    const userHmlContent = await ejs.renderFile(templatePath, {
      ...data,
      ...userExtraEmailData,
    });

    // Render the template with the provided data
    const htmlContent = await ejs.renderFile(templatePath, data);

    // Define the email content
    const userMsg = {
      to: booking.email,
      from: process.env.SENDER_MAIL, // Replace with your sender email
      subject: 'Booking Invoice',
      html: userHmlContent,
    };

    // Send User a email of Acceptance
    await sendConfirmationEmail(userMsg);

    // Check if signed in user associated with booking
    if (
      bookingUserAccount &&
      bookingUserAccount.email.toLowerCase() !==
        booking.email.toLowerCase().trim()
    ) {
      // Define the email content
      const bookingUserMsg = {
        to: bookingUserAccount.email,
        from: process.env.SENDER_MAIL, // Replace with your sender email
        subject: 'Booking Invoice',
        html: userHmlContent,
      };

      // Send User a email of Acceptance
      await sendConfirmationEmail(bookingUserMsg);
    }

    // Define the email content
    const driverMsg = {
      to: user.email,
      from: process.env.SENDER_MAIL, // Replace with your sender email
      subject: 'Booking Invoice',
      html: htmlContent,
    };

    await sendConfirmationEmail(driverMsg);

    await rideService.acceptBooking(bookingId, user.id, {
      trxId: booking.trxId,
    });

    const refreshedBooking = await rideService.fetchBooking(bookingId);

    res.json(refreshedBooking);
  } else {
    // Cancellation of Ride by Driver

    const cancelledBooking = await rideService.cancelRide(
      bookingId,
      user,
      'Driver declined booking'
    );

    await driverCancellation(res, bookingUserAccount, cancelledBooking);
  }
}

// Util for handling driver cancellation
async function driverCancellation(res, bookingUserAccount, booking) {
  // Email sending tell user it has being decline

  // Send user mail not accepted and they can re-book another car

  // Get current declined car , so it wont sho up on rebooking of other cars
  const carId = booking.car.id;

  // Get cars based on user booking details

  // booking details
  const { latitude: sourceLatitude, longitude: sourceLongitude } =
    booking.sourceLocation;

  const { latitude: destinationLatitude, longitude: destinationLongitude } =
    booking.destinationLocation;

  const scheduledDate = new Date(booking.departureTime).toISOString();

  const reBookingCars = await getCarsProximityUtil({
    res,
    sourceLatitude,
    sourceLongitude,
    destinationLatitude,
    destinationLongitude,
    state: booking.state,
    page: 1,
    pageSize: 5,
    scheduledDate,
  });

  // Remove already canceled car fromreBookingCars list sent

  const nextAvialableCars = reBookingCars.cars.filter(
    (carItem) => (carItem?.id).toString() !== carId
  );

  // Call html template and pass the car list
  // Render the template with the provided data

  // Check if the template file exists
  const canelRideTemplatePath = path.join(
    appRoot.path,
    'views',
    'driver-cancellation.ejs'
  );

  if (!fs.existsSync(canelRideTemplatePath)) {
    return res.status(404).json({ error: 'canel Ride Template not found' });
  }

  const cancelRideData = {
    cars: nextAvialableCars,
    booker: bookingUserAccount ? bookingUserAccount.fullName : booking.fullName,
    sourceLatitude,
    sourceLongitude,
    destinationLatitude,
    destinationLongitude,
    state: booking.state,
    scheduledDate: new Date(booking.departureTime).toISOString(),
    bookingId: booking.id,
    baseUrl: process.env.BASE_CLIENT_URL,
  };

  const cancelTripHtmlContent = await ejs.renderFile(
    canelRideTemplatePath,
    cancelRideData
  );

  // Define the email content
  if (
    bookingUserAccount &&
    bookingUserAccount.email.toLowerCase() !==
      booking.email.toLowerCase().trim()
  ) {
    // Define the email content
    const bookingUserMsg = {
      to: bookingUserAccount.email,
      from: process.env.SENDER_MAIL, // Replace with your sender email
      subject: 'Booking cancellation',
      html: cancelTripHtmlContent,
    };

    // Send User a email of Acceptance
    await sendConfirmationEmail(bookingUserMsg);
  }

  const userMsg = {
    to: booking.email,
    from: process.env.SENDER_MAIL, // Replace with your sender email
    subject: 'Booking cancellation',
    html: cancelTripHtmlContent,
  };

  await sendConfirmationEmail(userMsg);

  await booking.save();

  res.json(booking);
}

// Util fo handling booking creation
async function createBookingUtil(res, bookingData, user) {
  // Get car with its driver details
  const car = await Car.findOne({ _id: bookingData.car })
    .populate({
      path: 'driver',
    })
    .exec();

  // Edge case when Car does not exist
  if (!car) {
    return res.json('Car does not exist');
  }

  // Edge case when driver does not exist
  if (!car.driver) {
    return res.json('Driver does not exist');
  }

  // Create Booking
  const paymentDefaults = bookingData.payment || {
    method: bookingData.isCash ? 'cash' : 'card',
    status: bookingData.isCash ? 'pending' : 'paid',
  };

  const booking = new Booking({
    ...bookingData,
    payment: paymentDefaults,
    user: user,
  });

  await booking.save();

  // Create Mail token
  const generatedToken = generateIdToken({
    bookingId: booking._id,
  });

  const driverData = {
    name: car.driver.fullName,
    userName: booking.fullName,
    sourceAddress: booking.sourceLocation.address,
    destinationAddress: booking.destinationLocation.address,
    isPaid: booking.isCash ? 'Pending payment' : 'Paid',
    paymentType: booking.isCash ? 'Cash' : 'Online (paid)',
    amountPaid: booking.tripAmount,
    numberOfPassengers: booking.passengers,
    departureDate: convertDate(booking.departureTime),
    departureTime: convertTime(booking.departureTime),
    acceptanceLink: `${process.env.BASE_CLIENT_URL}/verify?rtoken=${generatedToken}`,
  };

  // Check if the template file exists
  const templatePath = path.join(
    appRoot.path,
    'views',
    'driver-acceptance.ejs'
  );

  if (!fs.existsSync(templatePath)) {
    return res.status(404).json({ error: 'Template not found' });
  }

  // Render the template with the provided data
  const htmlContent = await ejs.renderFile(templatePath, driverData);

  // Get driver email
  const driverEmail = car.driver.email;

  // Define the email content
  const msg = {
    to: driverEmail,
    from: process.env.SENDER_MAIL, // Replace with your sender email
    subject: 'Booking Acceptance',
    html: htmlContent,
  };

  // Send Driver a email of Acceptance
  await sendConfirmationEmail(msg);

  res.status(208).json({
    id: booking.id,
  });
}
