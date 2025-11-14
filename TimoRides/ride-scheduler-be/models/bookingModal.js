const { getLocation, getWallet } = require('../utils/modelGetterUtil');

const mongoose = require('mongoose');
const Schema = mongoose.Schema;
const { Decimal128 } = Schema.Types;

const BookingSchema = new mongoose.Schema(
  {
    user: { type: Schema.Types.ObjectId, ref: 'User' }, // Reference to User document
    car: { type: Schema.Types.ObjectId, ref: 'Car' }, // Reference to Car document
    departureTime: { type: Date, default: Date.now },
    tripAmount: { type: Decimal128, default: 0.0, get: getWallet },
    bookingType: {
      type: String,
      enum: ['goods', 'passengers'],
      default: 'passengers',
      required: true,
    },

    currency: {
      type: {
        symbol: {
          type: String,
        },
        code: {
          type: String,
        },
      },
    },

    tripDistance: {
      type: String,
    },

    tripDuration: {
      type: String,
    },

    state: {
      type: String,
    },

    passengers: {
      type: Number,
      default: 0,
    },

    fullName: {
      type: String,
      required: true,
    },
    phoneNumber: {
      type: String,
      required: true,
    },
    isCash: {
      type: Boolean,
      default: false,
    },

    email: {
      type: String,
      required: true,
    },

    isDriverAccept: {
      type: Boolean,
      default: false,
    },

    trxId: {
      type: String,
      default: null,
    },
    trxRef: {
      type: String,
      default: null,
    },
    status: {
      type: String,
      enum: ['pending', 'cancelled', 'completed', 'accepted', 'started'],
      default: 'pending',
    },
    timeline: {
      acceptedAt: Date,
      startedAt: Date,
      completedAt: Date,
      cancelledAt: Date,
    },
    payment: {
      method: {
        type: String,
        enum: ['cash', 'wallet', 'card', 'mobile_money'],
        default: 'cash',
      },
      status: {
        type: String,
        enum: ['unpaid', 'pending', 'paid', 'refunded'],
        default: 'unpaid',
      },
      reference: { type: String, default: null },
      notes: { type: String, default: '' },
      paidAt: { type: Date },
    },
    cancellation: {
      reason: { type: String, default: '' },
      cancelledBy: {
        id: { type: Schema.Types.ObjectId, ref: 'User' },
        role: { type: String },
        fullName: { type: String },
      },
    },
    destinationLocation: {
      type: {
        address: {
          type: String,
          required: true,
        },
        latitude: {
          type: Decimal128,
          required: true,
          get: getLocation,
        },
        longitude: {
          type: Decimal128,
          required: true,
          get: getLocation,
        },
      },
      required: true,
      _id: false,
    },

    sourceLocation: {
      type: {
        address: {
          type: String,
          required: true,
        },
        latitude: {
          type: Decimal128,
          required: true,
          get: getLocation,
        },
        longitude: {
          type: Decimal128,
          required: true,
          get: getLocation,
        },
      },
      required: true,
      _id: false,
    },
  },

  {
    timestamps: true,
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        delete ret._id;
        delete ret.__v;

        return ret;
      },
    },
  }
);

const Booking = mongoose.model('Booking', BookingSchema);

module.exports = Booking;
