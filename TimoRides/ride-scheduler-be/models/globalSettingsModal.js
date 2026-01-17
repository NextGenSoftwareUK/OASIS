const { getWallet } = require('../utils/modelGetterUtil');
const mongoose = require('mongoose');

const Schema = mongoose.Schema;

const { Decimal128 } = Schema.Types;

const GlobalSettingsSchema = new Schema(
  {
    pricePerKm: {
      type: Number,
      required: true,
      default: 10, // Default value can be adjusted as needed
    },
    driverWalletPercentage: {
      type: Decimal128,
      get: getWallet,
      required: true,
      default: 0.15, // Default value can be adjusted as needed
    },

    userPenalizeRate: {
      type: Decimal128,
      get: getWallet,
      required: true,
      default: 0.15, // Default value can be adjusted as needed
    },

    businessCommission: {
      type: Decimal128,
      get: getWallet,
      required: true,
      default: 0.15, // Default value can be adjusted as needed
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

const GlobalSettings = mongoose.model('GlobalSettings', GlobalSettingsSchema);

module.exports = GlobalSettings;
