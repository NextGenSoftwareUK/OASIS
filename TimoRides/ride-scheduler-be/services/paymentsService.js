const paystackGateway = require('./gateways/paystackGateway');
const Booking = require('../models/bookingModal');
const User = require('../models/userModel');
const PaymentRequest = require('../models/paymentRequestModel');
const { getWallet } = require('../utils/modelGetterUtil');
const { recordAuditLog } = require('./auditLogService');

class PaymentsService {
  /**
   * Fund wallet from Paystack payment
   * Called when Paystack webhook confirms a successful charge
   */
  async fundWalletFromPaystack({ bookingId, reference, amount, currency = 'ZAR' }) {
    try {
      // Verify transaction with Paystack
      const verification = await paystackGateway.verifyTransaction(reference);
      
      if (!verification.success) {
        console.error(`Paystack verification failed for ${reference}:`, verification.message);
        return {
          success: false,
          message: `Transaction verification failed: ${verification.message}`,
        };
      }

      const transaction = verification.data;
      
      // Ensure transaction is successful
      if (transaction.status !== 'success') {
        return {
          success: false,
          message: `Transaction status is ${transaction.status}, expected 'success'`,
        };
      }

      // Find booking by ID or reference
      let booking = null;
      if (bookingId) {
        // Validate ObjectId format before querying
        const mongoose = require('mongoose');
        if (mongoose.Types.ObjectId.isValid(bookingId)) {
          booking = await Booking.findById(bookingId);
        } else {
          console.warn(`Invalid bookingId format: ${bookingId}, trying to find by reference instead`);
        }
      }
      
      // If booking not found by ID, try to find by payment reference
      if (!booking) {
        booking = await Booking.findOne({ 'payment.reference': reference });
      }

      if (!booking) {
        console.warn(`Booking not found for reference ${reference}${bookingId ? ` or bookingId ${bookingId}` : ''}`);
        // Still return success to prevent Paystack retries, but log the issue
        return {
          success: true, // Return success to prevent webhook retries
          message: `Payment verified but booking not found for reference ${reference}`,
          warning: true,
        };
      }

      // Get user
      const user = await User.findById(booking.user);
      if (!user) {
        return {
          success: false,
          message: 'User not found',
        };
      }

      // Update booking payment status
      const paidAmount = transaction.amount / 100; // Paystack amounts are in kobo/cents
      booking.payment = {
        method: 'paystack',
        status: 'paid',
        reference: reference,
        paidAt: new Date(transaction.paid_at || Date.now()),
        amount: paidAmount,
        currency: currency,
      };

      await booking.save();

      // Record audit log
      await recordAuditLog({
        booking: { id: booking.id, status: booking.status },
        action: 'booking.payment.completed',
        actor: {
          id: user.id,
          role: user.role,
          email: user.email,
        },
        previous: { payment: { status: 'pending' } },
        current: { payment: booking.payment },
        metadata: {
          provider: 'paystack',
          reference: reference,
          transactionId: transaction.id,
        },
      });

      return {
        success: true,
        booking: booking,
        transaction: transaction,
      };
    } catch (error) {
      console.error('fundWalletFromPaystack error:', error);
      return {
        success: false,
        message: error.message,
        error: error,
      };
    }
  }

  /**
   * Process driver payout via Paystack
   * Creates transfer recipient if needed, then initiates transfer
   */
  async payoutViaPaystack({ paymentRequestId, driverId, amount, bankDetails }) {
    try {
      // Get payment request
      const paymentRequest = await PaymentRequest.findById(paymentRequestId);
      if (!paymentRequest) {
        return {
          success: false,
          message: 'Payment request not found',
        };
      }

      // Get driver
      const driver = await User.findById(driverId);
      if (!driver || driver.role !== 'driver') {
        return {
          success: false,
          message: 'Driver not found',
        };
      }

      // Check if driver has Paystack recipient code
      let recipientCode = driver.paystackRecipientCode;

      if (!recipientCode) {
        // Create transfer recipient
        const recipientData = {
          type: 'nuban', // Nigerian bank account (adjust for SA: 'nuban' or mobile money)
          name: driver.fullName || driver.email,
          account_number: bankDetails.account_number,
          bank_code: bankDetails.bank_code, // For SA, this might be different
          currency: 'ZAR',
        };

        const recipientResult = await paystackGateway.createTransferRecipient(recipientData);
        
        if (!recipientResult.success) {
          return {
            success: false,
            message: `Failed to create transfer recipient: ${recipientResult.message}`,
          };
        }

        recipientCode = recipientResult.data.recipient_code;
        
        // Save recipient code to driver profile
        driver.paystackRecipientCode = recipientCode;
        await driver.save();
      }

      // Initiate transfer
      const transferData = {
        source: process.env.PAYSTACK_TRANSFER_SOURCE || 'balance',
        amount: Math.round(amount * 100), // Convert to kobo/cents
        recipient: recipientCode,
        reason: `Driver payout for payment request ${paymentRequestId}`,
        currency: 'ZAR',
      };

      const transferResult = await paystackGateway.initiateTransfer(transferData);

      if (!transferResult.success) {
        return {
          success: false,
          message: `Transfer failed: ${transferResult.message}`,
        };
      }

      // Update payment request with transfer details
      paymentRequest.provider = 'paystack';
      paymentRequest.providerReference = transferResult.data.reference || transferResult.data.transfer_code;
      paymentRequest.status = 'processing';
      paymentRequest.statusHistory = paymentRequest.statusHistory || [];
      paymentRequest.statusHistory.push({
        status: 'processing',
        timestamp: new Date(),
        provider: 'paystack',
      });
      await paymentRequest.save();

      // Record audit log
      await recordAuditLog({
        booking: null,
        action: 'payment.payout.initiated',
        actor: {
          id: driver.id,
          role: 'driver',
          email: driver.email,
        },
        previous: { status: paymentRequest.status },
        current: { status: 'processing', providerReference: paymentRequest.providerReference },
        metadata: {
          provider: 'paystack',
          paymentRequestId: paymentRequestId,
          transferCode: transferResult.data.transfer_code,
        },
      });

      return {
        success: true,
        transfer: transferResult.data,
        paymentRequest: paymentRequest,
      };
    } catch (error) {
      console.error('payoutViaPaystack error:', error);
      return {
        success: false,
        message: error.message,
        error: error,
      };
    }
  }

  /**
   * Handle Paystack transfer webhook (success/failed)
   */
  async handleTransferWebhook({ event, data }) {
    try {
      const transfer = data;
      const transferCode = transfer.transfer_code || transfer.reference;

      // Find payment request by provider reference
      const paymentRequest = await PaymentRequest.findOne({
        providerReference: transferCode,
        provider: 'paystack',
      });

      if (!paymentRequest) {
        console.warn(`Payment request not found for transfer ${transferCode}`);
        return {
          success: false,
          message: 'Payment request not found',
        };
      }

      const previousStatus = paymentRequest.status;

      // Update status based on event
      if (event === 'transfer.success') {
        paymentRequest.status = 'completed';
      } else if (event === 'transfer.failed') {
        paymentRequest.status = 'failed';
        paymentRequest.failureReason = transfer.failure_reason || 'Transfer failed';
      }

      paymentRequest.statusHistory = paymentRequest.statusHistory || [];
      paymentRequest.statusHistory.push({
        status: paymentRequest.status,
        timestamp: new Date(),
        provider: 'paystack',
        event: event,
      });

      await paymentRequest.save();

      // Record audit log
      await recordAuditLog({
        booking: null,
        action: `payment.payout.${event.replace('transfer.', '')}`,
        actor: {
          id: 'system',
          role: 'system',
        },
        previous: { status: previousStatus },
        current: { status: paymentRequest.status },
        metadata: {
          provider: 'paystack',
          transferCode: transferCode,
          paymentRequestId: paymentRequest.id,
        },
      });

      return {
        success: true,
        paymentRequest: paymentRequest,
      };
    } catch (error) {
      console.error('handleTransferWebhook error:', error);
      return {
        success: false,
        message: error.message,
        error: error,
      };
    }
  }
}

module.exports = new PaymentsService();

