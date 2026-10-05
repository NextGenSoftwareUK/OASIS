const { getWallet } = require('../utils/modelGetterUtil');

// Model definition for users
const mongoose = require('mongoose');
const Schema = mongoose.Schema;
const { Decimal128 } = Schema.Types;

// Create schema

const userSchema = new mongoose.Schema(
  {
    title: {
      type: String,
      default: '',
    },

    fullName: {
      required: true,
      type: String,
    },

    profileImg: {
      type: String,
      default: '',
    },

    wallet: {
      type: Decimal128,
      default: 0.0,
      get: getWallet,
    },

    email: {
      required: true,
      unique: true,
      type: String,
    },

    phone: {
      required: true,
      type: String,
      unique: true,
    },

    password: {
      required: true,
      type: String,
    },

    homeAddress: {
      type: String,
      default: '',
    },

    isVerified: {
      default: false,
      type: Boolean,
    },

    role: {
      type: String,
      enum: ['admin', 'driver', 'user'],
      default: 'user',
    },

    isDisable: {
      //To disable account for 21 days and No schdules ride active incase user decides to delete acount
      type: Boolean,
      default: false,
    },

    // Next of kin Section
    nextOfKinFullName: {
      type: String,
      default: '',
    },

    nextOfKinEmailAddress: {
      type: String,
      default: '',
    },

    nextOfKinRelationship: {
      type: String,
      default: '',
    },

    nextOfKinPhoneCell: {
      type: String,
      default: '',
    },

    nextOfKinPhoneHome: {
      type: String,
      default: '',
    },

    nextOfKinHomeAddress: {
      type: String,
      default: '',
    },

    // Paystack integration fields
    paystackRecipientCode: {
      type: String,
      default: null,
    },
  },
  {
    timestamps: true,
    discriminatorKey: 'type',
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        delete ret._id;
        delete ret.__v; // Optionally remove other fields like __v
        return ret;
      },
    },
  }
);

const User = mongoose.model('User', userSchema);

module.exports = User;
