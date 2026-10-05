// Import the SendGrid client
require('dotenv').config({ path: '../config/.env' });
const sgMail = require('@sendgrid/mail');

// Function to send confirmation email
async function sendConfirmationEmail(msg) {
  try {
    // Set your SendGrid API key
    sgMail.setApiKey(process.env.SENDGRID_API_KEY);

    const { from: emailfrom } = msg;

    // Send the email
    await sgMail
      .send({
        ...msg,
        from: {
          email: emailfrom,
          name: 'Timo Rides',
        },
      })
      .then(() => {
        console.log('Confirmation email sent successfully');
      })
      .catch((error) => {
        console.error(error);
      });
  } catch (error) {
    console.error('Error sending confirmation email:', error);
    throw error; // Forward the error to the caller for handling
  }
}

module.exports = sendConfirmationEmail;
