// routes/userRoutes.js

const express = require('express');
const router = express.Router();
const authenticateUser = require('../middleware/authMiddleware.js');
const { userUpdateValidation } = require('../validators/userValidator.js');
const validatorResponse = require('../validators/validatorResponse.js');

const {
  getUsers,
  getUser,
  updateUser,
  deleteUser,
  requestPayment,
  topUpWallet,
  getWalletTransactions,
} = require('../controllers/userController.js');

// Define routes for user-related endpoints
router.post('/users/request-payment', authenticateUser, requestPayment);

router.post('/users/wallet-topup', authenticateUser, topUpWallet);

router.get(
  '/users/wallet-transaction',
  authenticateUser,
  getWalletTransactions
);

// General route
router.get('/users', authenticateUser, getUsers);

// Routes with :id
router.get('/users/:id', getUser);

router.put(
  '/users/:id',
  authenticateUser,
  userUpdateValidation,
  validatorResponse,
  updateUser
);

router.delete('/users/:id', authenticateUser, deleteUser);

// Add more routes as needed

module.exports = router;
