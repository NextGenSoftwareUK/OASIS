const { getLocation } = require('../utils/modelGetterUtil');

const mongoose = require('mongoose');

const Schema = mongoose.Schema;
const { Decimal128 } = Schema.Types;

const LocationSchema = new Schema(
  {
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
  {
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        delete ret._id;
        delete ret.id;

        return ret;
      },
    },
  }
);

const carSchema = new mongoose.Schema(
  {
    driver: {
      type: mongoose.Schema.Types.ObjectId,
      ref: 'Driver',
    },

    isOffline: {
      type: Boolean,
      default: false,
      index: true,
    },

    vehicleRegNumber: {
      type: String,
      required: true,
    },

    vehicleModelYear: {
      type: String,
      required: true,
    },

    vehicleMake: {
      type: String,
      required: true,
    },

    vehicleModel: {
      type: String,
      required: true,
    },

    engineNumber: {
      type: String,
      required: true,
    },

    vehicleColor: {
      type: String,
      required: true,
    },

    insuranceBroker: {
      type: String,
    },

    insurancePolicyNumber: {
      type: String,
    },

    imagePath: {
      type: String,
    },

    altImagePath: {
      type: String,
    },

    interiorImagePath: {
      type: String,
    },

    vehicleAddress: {
      type: String,
      required: true,
    },

    state: {
      type: String,
      required: true,
      index: true,
    },

    location: {
      type: LocationSchema,
      required: true,
    },

    rating: {
      type: Number,
      default: 5,
    },

    isVerify: {
      type: Boolean,
      default: false,
      index: true,
    },

    isActive: {
      type: Boolean,
      default: false,
      index: true,
    },
  },
  {
    timestamps: true,
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        let id = ret._id;
        delete ret._id;
        delete ret.__v;

        return { id, ...ret };
      },
    },
  }
);

const Car = mongoose.model('Car', carSchema);

module.exports = Car;
