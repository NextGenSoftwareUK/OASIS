// controllers/authController.js
require('dotenv').config({ path: '../config/.env' });
const jwt = require('jsonwebtoken');
const bcrypt = require('bcrypt');
const User = require('../models/userModel');
const driverModel = require('../models/driverModel');
const path = require('path');
const fs = require('fs');
const ejs = require('ejs');
const appRoot = require('app-root-path');
const sendConfirmationEmail = require('../utils/sendEmailUtil');
const oasisService = require('../services/oasisService');

const {
  generateAccessToken,
  generateRefreshToken,
  generateVerificationToken,
} = require('../utils/authUtil');
const { handleCatchError } = require('../utils/errorCatch');
const generateIdToken = require('../utils/genarateIdToken');

// Function to handle user login
async function login(req, res) {
  try {
    const { email, password } = req.body;

    // Find the user in the database

    const user = await User.findOne({ email });

    // Check if user exists and password is correct
    if (!user) {
      return res.status(401).json({ error: 'Invalid email or password' });
    }

    // Compare the provided password with the hashed password stored in the database
    const passwordMatch = await bcrypt.compare(password, user.password);

    if (!passwordMatch) {
      return res.status(401).json({ error: 'Invalid email or password' });
    }

    // Generate tokens
    const accessToken = generateAccessToken(user);
    const refreshToken = generateRefreshToken(user);

    const {
      password: _password,
      createdAt,
      updatedAt,
      __v,
      _id,
      ...exposeUserData
    } = user.toObject();

    // Fetch OASIS Karma if avatar exists
    let oasisData = null;
    if (user.oasisAvatarId) {
      try {
        const karma = await oasisService.getAvatarKarma(user.oasisAvatarId);
        oasisData = {
          avatarId: user.oasisAvatarId,
          karma: karma || user.oasisKarma || 0,
          verified: user.oasisVerified || false,
        };
      } catch (error) {
        console.error('Failed to fetch OASIS karma:', error.message);
        oasisData = {
          avatarId: user.oasisAvatarId,
          karma: user.oasisKarma || 0,
          verified: user.oasisVerified || false,
        };
      }
    }

    // Return tokens to the client
    res.json({
      accessToken,
      refreshToken,
      id: _id,
      ...exposeUserData,
      wallet: parseFloat(exposeUserData.wallet.toString()),
      oasis: oasisData,
    });
  } catch (error) {
    console.error('Error in login:', error);
    res.status(500).json({ error: 'Login failed. Please try again later.' });
  }
}

// Function to handle user signup
async function signup(req, res) {
  const { email, password, fullName, phone, userType } = req.body;

  try {
    const existingUser = await User.findOne({ email });

    if (existingUser) {
      return res.status(409).json({ error: 'Email already exist' });
    } else {
      // Data for Driver
      const driverData = {
        workPhone: '',
        homePhone: '',
        identityType: '',
        identityNumber: '',
        nextOfKinFullNameAlt: '',
        nextOfKinEmailAddressAlt: '',
        nextOfKinRelationshipAlt: '',
        nextOfKinPhoneCellAlt: '',
        nextOfKinHomeAddressAlt: '',
        nameOfAccountHoldeAltr: '',
        nameOfAccountContactPerson: '',
        nextOfKinPhoneHomeAlt: '',
        bankName: '',
        bankBranchName: '',
        bankBranchCode: '',
        bankAccountNumber: '',
        bankAccountType: '',
        bankAccountContactPerson: '',
        bankAuthorizedSignature: '',
        bankDate: new Date().toISOString(),
        bankNameInPrint: '',
        driverLicenseImg: '',
        driverIdentityCardImg: '',
        ComprehensiveInsuranceImg: '',
        OperatorCardImg: '',
        inspectionCertImg: '',
        ProfessionalDriverPermitImg: '',
        commissionConsent: false,
        commissionConsentAuthorizedSignatureImg: '',
        commissionConsentDate: new Date().toISOString(),
        commissionConsentNameInPrint: '',
        tcConsent: false,
        tcFullName: '',
        tcAuthorizedSignatureImg: '',
        tcDate: new Date().toISOString(),
        tcPlace: '',
        completedRides: 0,
        location: {
          latitude: 0,
          longitude: 0,
        },
      };

      const hashedPassword = await bcrypt.hash(password, 10);

      // Create a new user in the database
      const newUser =
        userType === 'driver'
          ? new driverModel({
              email,
              password: hashedPassword,
              fullName,
              phone,
              role: userType,
              ...driverData,
            })
          : new User({
              email,
              password: hashedPassword,
              fullName,
              phone,
              role: userType,
            });

      await newUser.save();

      // Create OASIS avatar (async, don't block signup)
      oasisService.getOrCreateAvatar({
        email,
        password,
        fullName,
        role: userType,
      }).then(async (avatar) => {
        if (avatar && avatar.id) {
          newUser.oasisAvatarId = avatar.id;
          newUser.oasisKarma = 0; // Start with 0 karma
          newUser.oasisVerified = false; // Will be verified after email confirmation
          await newUser.save();
          console.log(`✅ OASIS avatar created for ${email}: ${avatar.id}`);
        }
      }).catch((error) => {
        console.error('OASIS avatar creation failed (non-blocking):', error.message);
      });

      //  Generate Token
      const verificationToken = generateVerificationToken(email);

      // Check if the template file exists
      const templatePath = path.join(appRoot.path, 'views', 'verify-email.ejs');

      const data = {
        name: fullName,
        verificationLink: `${process.env.BASE_CLIENT_URL}/verify?token=${verificationToken}`,
      };

      if (!fs.existsSync(templatePath)) {
        return res.status(404).json({ error: 'Template not found' });
      }

      // Render the template with the provided data
      const htmlContent = await ejs.renderFile(templatePath, data);

      // Define the email content
      const msg = {
        to: email,
        from: process.env.SENDER_MAIL, // Replace with your sender email
        subject: 'Confirm Your Email Address',
        html: htmlContent,
      };

      // Send confirmation email
      await sendConfirmationEmail(msg);

      res
        .status(201)
        .json({ message: 'Signup successful. Confirmation email sent.' });
    }
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function reSendVerificationToken(req, res) {
  const { email } = req.body;

  const existingUser = await User.findOne({ email });

  if (!existingUser) {
    return res.status(404).json({ error: 'Email does not exist' });
  }

  try {
    //  Generate Token
    const verificationToken = generateVerificationToken(email);

    // Check if the template file exists
    const templatePath = path.join(appRoot.path, 'views', 'verify-email.ejs');

    const data = {
      name: existingUser.fullName,
      verificationLink: `${process.env.BASE_CLIENT_URL}/verify?token=${verificationToken}`,
    };

    if (!fs.existsSync(templatePath)) {
      return res.status(404).json({ error: 'Template not found' });
    }

    // Render the template with the provided data
    const htmlContent = await ejs.renderFile(templatePath, data);
    // Define the email content
    const msg = {
      to: email,
      from: process.env.SENDER_MAIL, // Replace with your sender email
      subject: 'Confirm Your Email Address',
      html: htmlContent,
    };

    // Send confirmation email
    await sendConfirmationEmail(msg);

    res.status(201).json({ message: 'Confirmation email sent.' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function refreshToken(req, res) {
  try {
    const { refreshToken } = req.body;

    // Verify the refresh token
    const decodedToken = jwt.verify(
      refreshToken,
      process.env.REFRESH_TOKEN_SECRET
    );

    // Check if the user exists in the database
    const user = await User.findById(decodedToken.userId);
    if (!user) {
      return res.status(401).json({ error: 'Unauthorized - User not found' });
    }

    // Generate tokens
    const accessToken = generateAccessToken(user);
    const newRefreshToken = generateRefreshToken(user);

    res.json({ accessToken, refreshToken: newRefreshToken });
  } catch (error) {
    console.error('Error refreshing token:', error);
    res.status(401).json({ error: 'Unauthorized - Invalid refresh token' });
  }
}

// Endpoint to verify the email using the token
async function verifyToken(req, res) {
  const { token } = req.body;

  try {
    // Find the user with the matching token
    // Verify the refresh token
    const decodedToken = jwt.verify(token, process.env.ACCESS_TOKEN_SECRET);

    const user = await User.findOne({ email: decodedToken.email });

    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }

    if (user.isVerified === true) {
      return res.status(404).json({
        message: 'You are already verified',
      });
    }

    // Perform additional actions like updating user status in the database, etc.
    // For demonstration purposes, let's just send a success message

    user.isVerified = true;

    user.save();

    res.status(200).json({ message: 'Email verified successfully' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function updatePassword(req, res) {
  const user = req.user;
  const { prevPassword, newPassword } = req.body;

  try {
    // Check if user exists and password is correct
    if (!user) {
      return res.status(401).json({ error: 'Invalid user' });
    }

    // Compare the provided password with the hashed password stored in the database
    const passwordMatch = await bcrypt.compare(prevPassword, user.password);

    if (!passwordMatch) {
      return res.status(401).json({ error: 'Invalid credentials' });
    }

    // Hash password
    const hashedPassword = await bcrypt.hash(newPassword, 10);

    const userToUpdate = await User.findByIdAndUpdate(
      user.id,
      { password: hashedPassword },
      { new: true }
    );

    if (!userToUpdate) {
      return res.status(500).json({ message: 'Update not successful' });
    }

    res.json({ message: 'password updated successfully' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function generateChangePasswordLink(req, res) {
  const { email } = req.body;
  try {
    // Check if email is in db
    const user = await User.findOne({ email: email });

    if (!user) {
      return res.status(404).json({ message: 'user not found' });
    }

    // create token withuser email and id
    //  Generate Token
    const verificationToken = generateIdToken({
      email: user.email,
      id: user.id,
    });

    // Define the email content
    const msg = {
      to: user.email,
      from: process.env.SENDER_MAIL, // Replace with your sender email
      subject: 'Change update',
      html: `<h1>Verify password update</h1>

          <p>Please click on the link below to change your password</p>

          <a href=' ${process.env.BASE_CLIENT_URL}/verify?token=${verificationToken}' target='_blank'>change password</a>
          `,
    };

    // Send link
    await sendConfirmationEmail(msg);

    res.json({ message: 'Password update link sent successfully' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

async function changePassword(req, res) {
  const { token, newPassword } = req.body;
  try {
    // Verify the refresh token
    const { email, id } = jwt.verify(
      token,
      process.env.VALIDATION_TOKEN_SECRET
    );

    const user = await User.findById(id);

    if (!user) {
      return res.status(404).json({ message: 'Invalid token' });
    }

    // Hash password
    const hashedPassword = await bcrypt.hash(newPassword, 10);

    const userToUpdate = await User.findByIdAndUpdate(
      user.id,
      { password: hashedPassword },
      { new: true }
    );

    if (!userToUpdate) {
      return res.status(500).json({ message: 'Update not successful' });
    }

    res.json({ message: 'password changed successfully' });
  } catch (error) {
    handleCatchError(error, res);
  }
}

module.exports = {
  login,
  signup,
  refreshToken,
  verifyToken,
  reSendVerificationToken,
  updatePassword,
  generateChangePasswordLink,
  changePassword,
};
