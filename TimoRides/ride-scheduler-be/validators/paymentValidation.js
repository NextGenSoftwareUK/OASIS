const { celebrate, Joi, Segments } = require('celebrate');

const paymentUpdateValidation = celebrate({
  [Segments.PARAMS]: Joi.object({
    id: Joi.string().hex().length(24).required(),
  }),
  [Segments.BODY]: Joi.object({
    method: Joi.string()
      .valid('cash', 'wallet', 'card', 'mobile_money')
      .optional(),
    status: Joi.string().valid('unpaid', 'pending', 'paid', 'refunded').required(),
    reference: Joi.string().allow(null, '').max(128),
    notes: Joi.string().allow(null, '').max(512),
  })
    .min(1)
    .messages({
      'object.min': 'At least one payment field must be provided',
    }),
});

module.exports = {
  paymentUpdateValidation,
};


