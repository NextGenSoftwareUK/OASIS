const Car = require('../models/carModal');
const GlobalSettings = require('../models/globalSettingsModal');
const paymentRequestModel = require('../models/paymentRequestModel');
const { handleCatchError } = require('../utils/errorCatch');

async function getGlobalSettings(req, res) {
  try {
    const globalSetting = await GlobalSettings.findOne({});

    res.json(globalSetting);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function updateGlobalSettings(req, res) {
  const {
    pricePerKm,
    driverWalletPercentage,
    businessCommission,
    userPenalizeRate,
  } = req.body;
  try {
    const settings = await GlobalSettings.findOneAndUpdate(
      {},
      {
        pricePerKm,
        driverWalletPercentage,
        businessCommission,
        userPenalizeRate,
      },
      { new: true, upsert: true } // `upsert: true` creates the document if it doesn't exist
    );

    res.status(200).json(settings);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function updateCarStatus(req, res) {
  const { isVerify, isActive, carId, ...otherData } = req.body;

  try {
    const car = await Car.findByIdAndUpdate(
      carId,
      { isVerify, isActive },
      { new: true }
    );

    res.status(200).json(car);
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function getpaymentRequest(req, res) {
  // Get all request

  const paymentRequests = await paymentRequestModel.find();

  res.json(paymentRequests);
}

async function confirmPaymentRequest(req, res) {
  const { id } = req.params;

  try {
    // Get Request payment by id and update status to completed
    const data = await paymentRequestModel.findByIdAndUpdate(
      id,
      {
        status: 'completed',
      },
      { new: true }
    );

    // Check if updated
    if (!data) return res.json({ message: 'Request id not updated' });

    res.json(data);
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  updateGlobalSettings,
  getGlobalSettings,
  updateCarStatus,
  getpaymentRequest,
  confirmPaymentRequest,
};
