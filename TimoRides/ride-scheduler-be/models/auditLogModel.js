const mongoose = require('mongoose');

const AuditLogSchema = new mongoose.Schema(
  {
    booking: {
      id: { type: mongoose.Schema.Types.ObjectId, ref: 'Booking' },
      status: { type: String },
    },
    action: {
      type: String,
      required: true,
    },
    actor: {
      id: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },
      role: { type: String },
      fullName: { type: String },
      email: { type: String },
    },
    previous: {
      type: mongoose.Schema.Types.Mixed,
    },
    current: {
      type: mongoose.Schema.Types.Mixed,
    },
    notes: { type: String },
    traceId: { type: String },
    source: { type: String, default: 'api' },
    metadata: {
      type: mongoose.Schema.Types.Mixed,
    },
  },
  {
    timestamps: true,
    toJSON: {
      transform: function (doc, ret) {
        const id = ret._id;
        delete ret._id;
        delete ret.__v;
        return { id, ...ret };
      },
    },
  }
);

const AuditLog = mongoose.model('AuditLog', AuditLogSchema);

module.exports = AuditLog;


