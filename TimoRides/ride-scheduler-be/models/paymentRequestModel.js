const { getWallet } = require('../utils/modelGetterUtil');
const mongoose = require('mongoose');

const Schema = mongoose.Schema;

const { Decimal128 } = Schema.Types;

const RequestPaymentSchema = new Schema(
  {
    userId: {
      type: String,
    },

    wallet: {
      type: Decimal128,
      get: getWallet,
      required: true,
    },

    amountRequested: {
      type: Decimal128,
      get: getWallet,
      required: true,
    },

    status: {
      type: String,
      enum: ['pending', 'completed', 'processing', 'failed'],
      default: 'pending',
    },

    role: {
      type: String,
    },

    // Paystack integration fields
    provider: {
      type: String,
      enum: ['paystack', 'mpesa', 'cash'],
      default: null,
    },

    providerReference: {
      type: String,
      default: null,
    },

    statusHistory: {
      type: Array,
      default: [],
    },

    failureReason: {
      type: String,
      default: null,
    },
  },
  {
    timestamps: true,
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        delete ret.__v;
        let id = ret._id;
        delete ret._id;
        return { id, ...ret };
      },
    },
  }
);

const RequestPayment = mongoose.model('RequestPayment', RequestPaymentSchema);

module.exports = RequestPayment;
