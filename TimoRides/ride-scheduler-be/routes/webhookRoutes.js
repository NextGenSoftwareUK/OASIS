const express = require('express');
const router = express.Router();
const paystackGateway = require('../services/gateways/paystackGateway');
const paymentsService = require('../services/paymentsService');
const { webhookLimiter } = require('../middleware/rateLimiters');

/**
 * Paystack Webhook Handler
 * Handles charge.success, transfer.success, transfer.failed events
 */
router.post(
  '/webhooks/paystack',
  webhookLimiter,
  async (req, res) => {
    try {
      // Get raw body for signature verification (captured by server.js middleware)
      const rawBody = req.rawBody || JSON.stringify(req.body);
      const signature = req.headers['x-paystack-signature'];

      // Verify webhook signature (skip in development if secret not set)
      const isDevelopment = process.env.NODE_ENV === 'development';
      const secretSet = !!process.env.PAYSTACK_WEBHOOK_SECRET && process.env.PAYSTACK_WEBHOOK_SECRET !== 'replace_me_after_first_webhook';
      
      if (secretSet && !paystackGateway.verifyWebhookSignature(rawBody, signature)) {
        console.error('⚠️  Invalid Paystack webhook signature');
        return res.status(401).json({
          success: false,
          message: 'Invalid webhook signature',
        });
      } else if (!secretSet && isDevelopment) {
        console.warn('⚠️  Skipping signature verification (development mode, secret not set)');
      }

      const event = req.body;

      // Log webhook event for debugging
      console.log(`📥 Paystack webhook received: ${event.event}`);

      // Handle different event types
      switch (event.event) {
        case 'charge.success': {
          const transaction = event.data;
          
          // Extract booking ID from metadata if provided
          const bookingId = transaction.metadata?.bookingId;
          const reference = transaction.reference;
          const amount = transaction.amount / 100; // Convert from kobo/cents
          const currency = transaction.currency || 'ZAR';

          const result = await paymentsService.fundWalletFromPaystack({
            bookingId,
            reference,
            amount,
            currency,
          });

          if (result.success) {
            return res.status(200).json({
              success: true,
              message: 'Payment processed successfully',
            });
          } else {
            console.error('Failed to process charge.success:', result.message);
            // Still return 200 to prevent Paystack retries, but log the error
            return res.status(200).json({
              success: false,
              message: result.message,
            });
          }
        }

        case 'transfer.success':
        case 'transfer.failed': {
          const result = await paymentsService.handleTransferWebhook({
            event: event.event,
            data: event.data,
          });

          if (result.success) {
            return res.status(200).json({
              success: true,
              message: `Transfer ${event.event} processed`,
            });
          } else {
            console.error(`Failed to process ${event.event}:`, result.message);
            return res.status(200).json({
              success: false,
              message: result.message,
            });
          }
        }

        default:
          console.log(`ℹ️  Unhandled Paystack event: ${event.event}`);
          // Return 200 for unhandled events to prevent retries
          return res.status(200).json({
            success: true,
            message: `Event ${event.event} received but not processed`,
          });
      }
    } catch (error) {
      console.error('Paystack webhook error:', error);
      // Return 200 to prevent Paystack from retrying
      return res.status(200).json({
        success: false,
        message: error.message,
      });
    }
  }
);

module.exports = router;

