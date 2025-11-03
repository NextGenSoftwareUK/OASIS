const mongoose = require('mongoose');

const Schema = mongoose.Schema;

const TripSchema = new Schema(
  {
    codeDate: { type: Date, default: Date.now },
    code: {
      type: String,
      required: true,
    },
    isActive: {
      type: Boolean,
      default: false,
    },
  },
  {
    toJSON: {
      transform: function (doc, ret, options) {
        delete ret._id;

        return ret;
      },
    },
  }
);

const CancellerSchema = new Schema(
  {
    id: {
      type: String,
    },
    role: {
      type: String,
    },
  },
  {
    toJSON: {
      transform: function (doc, ret, options) {
        delete ret._id;

        return ret;
      },
    },
  }
);

const CancellationShema = new Schema(
  {
    cancelledDate: { type: Date, default: Date.now },
    cancelledBy: {
      type: CancellerSchema,
    },
    isCancelled: {
      type: Boolean,
      default: false,
    },
  },
  {
    toJSON: {
      transform: function (doc, ret, options) {
        delete ret._id;

        return ret;
      },
    },
  }
);

// Otp Schema
const OtpSchema = new Schema(
  {
    bookingId: { type: Schema.Types.ObjectId, ref: 'Booking' }, // Reference to Booking document
    driverId: { type: Schema.Types.ObjectId, ref: 'Driver' },
    startTrip: { type: TripSchema },
    endTrip: { type: TripSchema },
    cancelledTrip: { type: CancellationShema },
    isPenalized: {
      type: Boolean,
      default: false,
    },
  },
  {
    timestamps: true,
    toJSON: {
      transform: function (doc, ret, options) {
        delete ret.__v;
        let id = ret._id;
        delete ret._id;
        return { id, ...ret };
      },
    },
  }
);

const Otp = mongoose.model('Otp', OtpSchema);

module.exports = Otp;
