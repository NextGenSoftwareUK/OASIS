const axios = require('axios');
const crypto = require('crypto');

class PaystackGateway {
  constructor() {
    this.secretKey = process.env.PAYSTACK_SECRET_KEY;
    this.publicKey = process.env.PAYSTACK_PUBLIC_KEY;
    this.baseUrl = process.env.PAYSTACK_BASE_URL || 'https://api.paystack.co';
    this.webhookSecret = process.env.PAYSTACK_WEBHOOK_SECRET;
    
    if (!this.secretKey) {
      console.warn('⚠️  PAYSTACK_SECRET_KEY not set in environment');
    }
  }

  /**
   * Get authorization headers for Paystack API
   */
  getHeaders() {
    return {
      Authorization: `Bearer ${this.secretKey}`,
      'Content-Type': 'application/json',
    };
  }

  /**
   * Verify Paystack webhook signature
   */
  verifyWebhookSignature(rawBody, signature) {
    if (!this.webhookSecret) {
      console.warn('⚠️  PAYSTACK_WEBHOOK_SECRET not set - skipping signature verification');
      return true; // Allow in development if secret not set
    }

    const hash = crypto
      .createHmac('sha512', this.webhookSecret)
      .update(rawBody)
      .digest('hex');

    return hash === signature;
  }

  /**
   * Verify a transaction by reference
   * @param {string} reference - Paystack transaction reference
   * @returns {Promise<Object>} Transaction details
   */
  async verifyTransaction(reference) {
    try {
      const response = await axios.get(
        `${this.baseUrl}/transaction/verify/${reference}`,
        { headers: this.getHeaders() }
      );

      if (response.data.status && response.data.data) {
        return {
          success: true,
          data: response.data.data,
        };
      }

      return {
        success: false,
        message: response.data.message || 'Transaction verification failed',
      };
    } catch (error) {
      console.error('Paystack verifyTransaction error:', error.response?.data || error.message);
      return {
        success: false,
        message: error.response?.data?.message || error.message,
        error: error.response?.data,
      };
    }
  }

  /**
   * Create a transfer recipient (bank account or mobile money)
   * @param {Object} recipientData - { type, name, account_number, bank_code, currency }
   * @returns {Promise<Object>} Recipient details
   */
  async createTransferRecipient(recipientData) {
    try {
      const response = await axios.post(
        `${this.baseUrl}/transferrecipient`,
        recipientData,
        { headers: this.getHeaders() }
      );

      if (response.data.status && response.data.data) {
        return {
          success: true,
          data: response.data.data,
        };
      }

      return {
        success: false,
        message: response.data.message || 'Failed to create transfer recipient',
      };
    } catch (error) {
      console.error('Paystack createTransferRecipient error:', error.response?.data || error.message);
      return {
        success: false,
        message: error.response?.data?.message || error.message,
        error: error.response?.data,
      };
    }
  }

  /**
   * Initiate a transfer to a recipient
   * @param {Object} transferData - { source, amount, recipient, reason, currency }
   * @returns {Promise<Object>} Transfer details
   */
  async initiateTransfer(transferData) {
    try {
      const response = await axios.post(
        `${this.baseUrl}/transfer`,
        transferData,
        { headers: this.getHeaders() }
      );

      if (response.data.status && response.data.data) {
        return {
          success: true,
          data: response.data.data,
        };
      }

      return {
        success: false,
        message: response.data.message || 'Transfer initiation failed',
      };
    } catch (error) {
      console.error('Paystack initiateTransfer error:', error.response?.data || error.message);
      return {
        success: false,
        message: error.response?.data?.message || error.message,
        error: error.response?.data,
      };
    }
  }

  /**
   * Get transfer details by code or ID
   * @param {string} transferCode - Transfer code or ID
   * @returns {Promise<Object>} Transfer details
   */
  async getTransfer(transferCode) {
    try {
      const response = await axios.get(
        `${this.baseUrl}/transfer/${transferCode}`,
        { headers: this.getHeaders() }
      );

      if (response.data.status && response.data.data) {
        return {
          success: true,
          data: response.data.data,
        };
      }

      return {
        success: false,
        message: response.data.message || 'Failed to fetch transfer',
      };
    } catch (error) {
      console.error('Paystack getTransfer error:', error.response?.data || error.message);
      return {
        success: false,
        message: error.response?.data?.message || error.message,
        error: error.response?.data,
      };
    }
  }
}

module.exports = new PaystackGateway();


