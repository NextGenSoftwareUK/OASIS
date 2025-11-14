// Import the otp-generator library
const otpGenerator = require('otp-generator');

// Function to generate an OTP
function generateOtp(numberOfOtp) {
  // Generate a 6-digit OTP with only numbers
  const startOtpCode = otpGenerator.generate(6, {
    upperCaseAlphabets: false,
    specialChars: false,
  });

  const endOtpCode = otpGenerator.generate(6, {
    upperCaseAlphabets: false,
    specialChars: false,
  });

  return { startOtpCode, endOtpCode };
}

module.exports = generateOtp;
