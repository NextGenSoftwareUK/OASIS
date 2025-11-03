// Download the helper library from https://www.twilio.com/docs/node/install
// Set environment variables for your credentials
// Read more at http://twil.io/secure
require('dotenv').config({ path: '../config/.env' });

const authToken = process.env.TWILIO_AUTH_TOKEN;
const accountSid = process.env.TWILIO_ACCOUNT_SID;
const verifySid = process.env.TWILIO_VERIFY_SID;
const client = require('twilio')(accountSid, authToken);

async function sendOtp(phoneNumber, otpChannel) {
  await client.verify.v2
    .services(verifySid)
    .verifications.create({ to: phoneNumber, channel: otpChannel })
    .then((verification) => console.log(verification.status))
    .then(() => {
      console.log('Sent successfully ');
    });
}

async function verifyOtp(phoneNumber, otpCode) {
  await client.verify.v2
    .services(verifySid)
    .verificationChecks.create({ to: phoneNumber, code: otpCode })
    .then((verification_check) => console.log(verification_check.status));
}

module.exports = { verifyOtp, sendOtp };
