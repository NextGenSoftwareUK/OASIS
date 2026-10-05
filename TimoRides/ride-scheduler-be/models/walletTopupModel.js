const { getWallet } = require('../utils/modelGetterUtil');
const mongoose = require('mongoose');

const Schema = mongoose.Schema;

const { Decimal128 } = Schema.Types;

const TopUpWalletSchema = new Schema(
  {
    userId: {
      type: String,
    },

    trxId: {
      type: String,
    },

    trxRef: {
      type: String,
    },

    topUpAmount: {
      type: Decimal128,
      get: getWallet,
      required: true,
    },

    prevWalletAmount: {
      type: Decimal128,
      get: getWallet,
      required: true,
    },

    role: {
      type: String,
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

const TopUpWallet = mongoose.model('TopUpWallet', TopUpWalletSchema);

module.exports = TopUpWallet;
