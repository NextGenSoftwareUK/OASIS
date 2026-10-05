// Download the helper library from https://www.twilio.com/docs/node/install
// Set environment variables for your credentials
// Read more at http://twil.io/secure
require('dotenv').config({ path: '../config/.env' });

const authToken = process.env.TWILIO_AUTH_TOKEN;
const accountSid = process.env.TWILIO_ACCOUNT_SID;
const verifySid = process.env.TWILIO_VERIFY_SID;

const isTwilioConfigured =
  accountSid && accountSid.startsWith('AC') && authToken && verifySid;

let client = null;

if (isTwilioConfigured) {
  client = require('twilio')(accountSid, authToken);
} else {
  console.warn(
    'Twilio credentials not configured. SMS/OTP will be skipped (dev mode).'
  );
}

async function sendOtp(phoneNumber, otpChannel) {
  if (!isTwilioConfigured) {
    console.log(
      `[SMS] Skipping OTP send to ${phoneNumber} via ${otpChannel} (Twilio disabled).`
    );
    return;
  }

  await client.verify.v2
    .services(verifySid)
    .verifications.create({ to: phoneNumber, channel: otpChannel })
    .then((verification) => console.log(verification.status))
    .then(() => {
      console.log('Sent successfully ');
    });
}

async function verifyOtp(phoneNumber, otpCode) {
  if (!isTwilioConfigured) {
    console.log(
      `[SMS] Skipping OTP verification for ${phoneNumber} (Twilio disabled).`
    );
    return;
  }

  await client.verify.v2
    .services(verifySid)
    .verificationChecks.create({ to: phoneNumber, code: otpCode })
    .then((verification_check) => console.log(verification_check.status));
}

module.exports = { verifyOtp, sendOtp };
