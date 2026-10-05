// Controllers for handling user-related logic

const userModel = require('../models/userModel.js');
const driverModel = require('../models/driverModel.js');
const paymentRequestModel = require('../models/paymentRequestModel.js');

const { handleCatchError } = require('../utils/errorCatch.js');
const Car = require('../models/carModal.js');
const { getCurrectActiveCar } = require('../utils/carUtil.js');
const { getWallet } = require('../utils/modelGetterUtil.js');
const User = require('../models/userModel.js');
const TopUpWallet = require('../models/walletTopupModel.js');

async function getUsers(req, res) {
  const { role } = req.query;

  if (req.user.role === 'admin') {
    const allUsers = !role
      ? await userModel.find().select('-password')
      : ['user', 'driver'].includes(role.toLowerCase())
      ? await userModel.find({ role: role.toLowerCase() }).select('-password')
      : await userModel.find().select('-password');

    res.status(200).json({ data: allUsers });
  } else {
    res.status(405).json({ message: 'Unauthorized' });
  }
}

async function getUser(req, res) {
  const { id } = req.params;

  try {
    let user = await userModel.findById(id).select('-password');

    if (!user) {
      return res.status(404).json({ message: 'User not found' });
    }

    // To allow wallet display properly
    const modifiedUser = {
      ...user._doc,
      wallet: getWallet(user._doc.wallet),
      id: user._doc._id,
    };

    const { _id, __v, ...otherUserDetails } = modifiedUser;

    if (otherUserDetails.role === 'driver') {
      const cars = await Car.find({ driver: id });

      // initialization of car
      let car = [];

      // If no cars for driver
      if (!cars || cars.length === 0) {
        car = null;
      } else {
        // set car since cars is not null
        // assumig cars is of lenth 1
        car = cars[0];
      }

      // if car is more than length one then get acyive car from lis and set accordingly
      if (cars.length > 1) {
        // current car
        car = getCurrectActiveCar(cars)[0];
      }

      return res.json({
        data: { ...otherUserDetails, car: car },
      });
    }

    res.json({ data: otherUserDetails });
  } catch (err) {
    res.status(404).json({ data: err.message });
  }
}

async function updateUser(req, res) {
  const { id } = req.params;

  const user = req.user;

  if (req.user._id.toString() !== id && req.user.role !== 'admin') {
    return res.status(405).json({ message: 'Unauthorized' });
  }

  try {
    const userToUpdate = await userModel.findById(id);

    if (!userToUpdate) {
      return res.status(200).json({ message: 'User not found' });
    }

    if (user.role !== 'admin') {
      // owner of account (user or driver)
      // Exclude certain fields from the request body that should not be updated
      const {
        fullName,
        createdBy,
        updatedAt,
        email,
        isVerified,
        role,
        userType,
        wallet,
        type,
        _id,
        isDisable,
        completedRides,
        passsword,
        ...updatedData
      } = req.body;

      const updatedUser =
        userToUpdate.role === 'driver'
          ? await driverModel
              .findByIdAndUpdate(id, updatedData, {
                new: true,
                runValidators: true,
              })
              .select('-password')
          : await userModel
              .findByIdAndUpdate(id, updatedData, {
                new: true,
                runValidators: true,
              })
              .select('-password');

      res.json(updatedUser);
    } else {
      // For admin
      const { createdBy, wallet, updatedAt, userType, _id, ...updatedData } =
        req.body;

      const updatedUser =
        userToUpdate.role === 'driver'
          ? await driverModel.findByIdAndUpdate(id, updatedData, {
              new: true,
              runValidators: true,
            })
          : await userModel.findByIdAndUpdate(id, updatedData, {
              new: true,
              runValidators: true,
            });

      res.json(updatedUser);
    }
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function deleteUser(req, res) {
  const { id } = req.params;

  if (req.user._id.toString() === id) {
    try {
      const user = await userModel.findById(id);

      if (!user) {
        return res.status(404).json({ message: 'User not found' });
      }

      user.isDisable = true;

      user.save();

      res.json({
        message: 'Your account has been disabled and will deleted in 30 days',
      });
    } catch {
      return res.status(404).json({ data: err.message });
    }
  } else {
    res.status(405).json({ message: 'Unauthorized' });
  }
}

// User request payment
async function requestPayment(req, res) {
  // Get user requesting payment
  const user = req.user;

  // Get amount requested
  const { amount } = req.body;

  // Check if amount can be withdrawn
  if (amount > user?.wallet || amount <= 0) {
    return res.status(405).json({ message: 'Can not withdraw amount' });
  }

  // Proceed with payment request
  const paymentRequestData = {
    userId: user?.id,
    wallet: user?.wallet,
    amountRequested: amount,
    status: 'pending',
    role: user?.role,
  };

  const paymentRequest = new paymentRequestModel(paymentRequestData);

  // Deduct amount from user wallet
  const newWalletPrice = user?.wallet - amount;

  const userData = await User.findByIdAndUpdate(
    user?.id,
    { wallet: newWalletPrice },
    { new: true }
  );

  if (!userData) return res.json({ message: 'Account not updated' });

  await paymentRequest.save();

  res.json({
    id: user?.id,
    wallet: newWalletPrice,
    message: 'Payment request is being processed',
  });
}

// Get payment transaction partaining to user
async function getWalletTransactions(req, res) {
  const user = req.user;

  try {
    const paymentTransactions = await paymentRequestModel
      .find({ userId: user?.id.toString() })
      .lean();

    const topUpTransactions = await TopUpWallet.find({
      userId: user?.id.toString(),
    }).lean();

    console.log(paymentTransactions, ' pay -- ');

    const modifiedPaymentRequestTransactions = paymentTransactions.map(
      (transaction) => {
        const { _id, __v, ...restData } = transaction;

        return {
          ...restData,
          id: _id,
          amountRequested: getWallet(restData?.amountRequested),
          wallet: getWallet(restData.wallet),
          type: 'withdrawal',
        };
      }
    );

    const modifiedTopUpTransactions = topUpTransactions.map((transaction) => {
      const { _id, __v, ...restData } = transaction;

      return {
        ...restData,
        id: _id,
        topUpAmount: getWallet(restData?.topUpAmount),
        prevWalletAmount: getWallet(restData.prevWalletAmount),
        type: 'topup',
      };
    });

    res.json([
      ...modifiedPaymentRequestTransactions,
      ...modifiedTopUpTransactions,
    ]);
  } catch (error) {
    handleCatchError(error, res);
  }
}

// User wallet top up
async function topUpWallet(req, res) {
  // Get data details
  const { trxId, trxRef, amount } = req.body;

  try {
    // Get user
    const user = req.user;

    // create new top wallet data
    const topUpWalletData = {
      trxId,
      trxRef,
      userId: user?.id,
      prevWalletAmount: user?.wallet,
      role: user?.role,
      topUpAmount: amount,
    };

    const topUpWallet = new TopUpWallet(topUpWalletData);

    // Increament user wallet
    const newWalletAmount = user?.wallet + amount;

    const userUpdate = await User.findByIdAndUpdate(
      user?.id,
      {
        wallet: newWalletAmount,
      },
      { new: true }
    );

    // Check if update occur
    if (!userUpdate) return res.json({ message: 'Wallet not updated ' });

    // Save top up wallet data
    await topUpWallet.save();

    res.json({
      id: user?.id,
      wallet: newWalletAmount,
      message: 'wallet top up successful',
    });
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  getUsers,
  getUser,
  updateUser,
  deleteUser,
  requestPayment,
  topUpWallet,
  getWalletTransactions,
};
