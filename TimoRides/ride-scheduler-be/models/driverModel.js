// driverModel.js
// Inherits properties from the User model

const { getLocation } = require('../utils/modelGetterUtil');
const mongoose = require('mongoose');
const User = require('./userModel');

const Schema = mongoose.Schema;

const { Decimal128 } = Schema.Types;

// Sub document
const locationSchema = new Schema(
  {
    latitude: {
      type: Decimal128,
      get: getLocation,
      required: true,
    },
    longitude: {
      type: Decimal128,
      get: getLocation,
      required: true,
    },
  },
  {
    _id: false,
    toJSON: {
      getters: true,
      transform: function (doc, ret, options) {
        delete ret._id;

        return ret;
      },
    },
  }
);

const driverSchema = new mongoose.Schema({
  // Add more fields as needed

  // Identity section

  workPhone: {
    type: String,
    default: '',
  },

  homePhone: {
    type: String,
    default: '',
  },

  identityType: {
    type: String,
    default: '',
  },

  identityNumber: {
    type: String,
    default: '',
  },

  // Next of kin Section
  nextOfKinFullNameAlt: {
    type: String,
    default: '',
  },

  nextOfKinEmailAddressAlt: {
    type: String,
    default: '',
  },

  nextOfKinRelationshipAlt: {
    type: String,
    default: '',
  },
  nextOfKinPhoneCellAlt: {
    type: String,
    default: '',
  },

  nextOfKinPhoneHomeAlt: {
    type: String,
    default: '',
  },

  nextOfKinHomeAddressAlt: {
    type: String,
    default: '',
  },

  // Bank Acount section

  nameOfAccountHolder: {
    type: String,
    default: '',
  },

  nameOfAccountContactPerson: {
    type: String,
    default: '',
  },

  bankName: {
    type: String,
    default: '',
  },
  bankBranchName: {
    type: String,
    default: '',
  },
  bankBranchCode: {
    type: String,
    default: '',
  },
  bankAccountNumber: {
    type: String,
    default: '',
  },
  bankAccountType: {
    type: String,
    default: '',
  },

  bankAccountContactPerson: {
    type: String,
    default: '',
  },

  bankAuthorizedSignature: {
    type: String,
    default: '',
  },

  bankDate: {
    type: Date,
  },

  bankNameInPrint: {
    type: String,
    default: '',
  },

  // Image data
  driverLicenseImg: {
    type: String,
    default: '',
  },

  driverIdentityCardImg: {
    type: String,
    default: '',
  },

  comprehensiveInsuranceImg: {
    type: String,
    default: '',
  },

  operatorCardImg: {
    type: String,
    default: '',
  },
  inspectionCertImg: {
    type: String,
    default: '',
  },
  professionalDriverPermitImg: {
    type: String,
    default: '',
  },

  // Commmission Conduct
  commissionConsent: {
    type: Boolean,
    default: false,
  },

  commissionConsentAuthorizedSignatureImg: {
    type: String,
    default: '',
  },

  commissionConsentDate: {
    type: Date,
  },

  commissionConsentNameInPrint: {
    type: String,
    default: '',
  },

  // Terms and condition
  tcConsent: {
    type: Boolean,
    default: false,
  },

  tcFullName: {
    type: String,
    default: '',
  },
  tcAuthorizedSignatureImg: {
    type: String,
    default: '',
  },
  tcDate: {
    type: Date,
  },
  tcPlace: {
    type: String,
    default: '',
  },

  completedRides: {
    type: Number,
    default: 0,
  },

  location: {
    type: locationSchema,
    required: true,
  },
  heading: {
    type: Number,
    default: 0,
  },
  speed: {
    type: Number,
    default: 0,
  },
});

const Driver = User.discriminator('Driver', driverSchema);

module.exports = Driver;
