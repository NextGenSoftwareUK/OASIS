const express = require('express');
const authenticateUser = require('../middleware/authMiddleware.js');
const setUserMiddleWare = require('../middleware/setUserMiddleWare.js');
const {
  getAllBooking,
  createBooking,
  reCreateBooking,
  verifyAcceptance,
  getBooking,
  acceptBooking,
  cancelBooking,
} = require('../controllers/bookingController');

const {
  bookingValidation,
  reBookingValidation,
  validate,
} = require('../validators/bookingValidation');

const router = express.Router();

router.post('/', setUserMiddleWare, bookingValidation, validate, createBooking);

router.post(
  '/re-book',
  setUserMiddleWare,
  reBookingValidation,
  validate,
  reCreateBooking
);

router.get('/', authenticateUser, getAllBooking);

router.get('/:id', authenticateUser, getBooking);

router.post('/verify-acceptance', authenticateUser, verifyAcceptance);

router.post('/confirm-acceptance-status', authenticateUser, acceptBooking);

router.post('/cancel-acceptance', authenticateUser, cancelBooking);

module.exports = router;
