const sendConfirmationEmail = require('../utils/sendEmailUtil');
const path = require('path');
const fs = require('fs');
const ejs = require('ejs');

const appRoot = require('app-root-path');
const { handleCatchError } = require('../utils/errorCatch');
const { sendOtp, verifyOtp } = require('../utils/sendSms');

async function emailNotification(req, res) {
  const { subject, recipient, data, templateName } = req.body;

  if (subject.trim() !== '' && recipient.trim() !== '') {
    // Check if the template file exists
    const templatePath = path.join(
      appRoot.path,
      'views',
      templateName + '.ejs'
    );

    if (!fs.existsSync(templatePath)) {
      return res.status(404).json({ error: 'Template not found' });
    }

    /**
     * For Invoice data is
     * {
     * "name": "string",
     * "phone": "string",
     * "pickup": "string",
     * "dropoff": "string",
     * "scheduledDate": "string (YYYY-MM-DD)",
     * "scheduledTime": "string (HH:MM AM/PM)",
     * "typeOfRide": "string",
     * "driverName": "string (optional)",
     * "driverPhone": "string (optional)",
     * "vehicleNumber": "string (optional)",
     * "transactionRef": "string (optional)",
     * "pricePaid": "number (optional)"
     *
     * -- For Welcome template
     * {
     * "name": string
     * }
  }
     */

    // Render the template with the provided data
    const htmlContent = await ejs.renderFile(templatePath, data);

    // Define the email content
    const msg = {
      to: recipient,
      subject: subject,
      html: htmlContent,
    };

    await sendConfirmationEmail(msg);

    res.status(200).json({ message: 'Email sent successfully' });
  } else {
    res.status(400).json({ message: 'Required fields are empty' });
  }
}

async function sendUserOtp(req, res) {
  const { phoneNumber, channel } = req.body;
  try {
    const otpChannel = !channel ? 'sms' : channel.toLowerCase();

    await sendOtp(phoneNumber, otpChannel);

    res.json({ message: 'Otp Sent successfuly' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function verifyUserOtp(req, res) {
  const { phoneNumber, otpCode } = req.body;
  try {
    await verifyOtp(phoneNumber, otpCode);

    res.json({ message: 'Otp Verified successfuly' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = { emailNotification, sendUserOtp, verifyUserOtp };
