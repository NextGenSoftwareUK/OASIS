const { body, validationResult } = require('express-validator');

// Validation middleware
const userUpdateValidation = [
  body('title')
    .optional()
    .notEmpty()
    .withMessage('title must not be empty')
    .isString()
    .withMessage('title must be a string'),

  body('profileImg')
    .optional()
    .notEmpty()
    .withMessage('profileImg must not be empty')
    .isString()
    .withMessage('profileImg must be a string'),

  body('phone')
    .optional()
    .notEmpty()
    .withMessage('Phone number is required')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('Phone number must be between 10 and 15 digits'),

  body('homeAddress')
    .optional()
    .notEmpty()
    .withMessage('homeAddress must not be empty')
    .isString()
    .withMessage('homeAddress must be a string'),

  body('nextOfKinFullName')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinFullName must not be empty')
    .isString()
    .withMessage('nextOfKinFullName must be a string'),

  body('nextOfKinEmailAddress')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinEmailAddress must not be empty')
    .isEmail()
    .withMessage('nextOfKinEmailAddress must be a email'),

  body('nextOfKinRelationship')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinRelationship must not be empty')
    .isString()
    .withMessage('nextOfKinRelationship must be a string'),

  body('nextOfKinPhoneCell')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinPhoneCell must not be empty')
    .isString()
    .withMessage('nextOfKinPhoneCell must be a string'),

  body('nextOfKinPhoneHome')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinPhoneHome must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('nextOfKinPhoneHome must be between 10 and 15 digits'),

  body('nextOfKinHomeAddress')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinHomeAddress must not be empty')
    .isString()
    .withMessage('nextOfKinHomeAddress must be a string'),

  body('workPhone')
    .optional()
    .notEmpty()
    .withMessage('workPhone must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('workPhone must be between 10 and 15 digits'),

  body('homePhone')
    .optional()
    .notEmpty()
    .withMessage('homePhone must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('homePhone must be between 10 and 15 digits'),

  body('identityType')
    .optional()
    .notEmpty()
    .withMessage('identityType must not be empty')
    .isString()
    .withMessage('identityType must be a string'),

  body('identityNumber')
    .optional()
    .notEmpty()
    .withMessage('identityNumber must not be empty')
    .isString()
    .withMessage('identityNumber must be a string'),

  body('nextOfKinFullNameAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinFullNameAlt must not be empty')
    .isString()
    .withMessage('nextOfKinFullNameAlt must be a string'),

  body('nextOfKinEmailAddressAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinEmailAddressAlt must not be empty')
    .isEmail()
    .withMessage('nextOfKinEmailAddressAlt must be a email'),

  body('nextOfKinRelationshipAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinRelationshipAlt must not be empty')
    .isString()
    .withMessage('nextOfKinRelationshipAlt must be a string'),

  body('nextOfKinPhoneCellAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinPhoneCellAlt must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('nextOfKinPhoneCellAlt must be between 10 and 15 digits'),

  body('nextOfKinPhoneHomeAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinPhoneHomeAlt must not be empty')
    .matches(/^\+?[0-9]{10,15}$/)
    .withMessage('nextOfKinPhoneHomeAlt must be between 10 and 15 digits'),

  body('nextOfKinHomeAddressAlt')
    .optional()
    .notEmpty()
    .withMessage('nextOfKinHomeAddressAlt must not be empty')
    .isString()
    .withMessage('nextOfKinHomeAddressAlt must be a string'),

  body('nameOfAccountHolder')
    .optional()
    .notEmpty()
    .withMessage('nameOfAccountHolder must not be empty')
    .isString()
    .withMessage('nameOfAccountHolder must be a string'),

  body('nameOfAccountContactPerson')
    .optional()
    .notEmpty()
    .withMessage('nameOfAccountContactPerson must not be empty')
    .isString()
    .withMessage('nameOfAccountContactPerson must be a string'),

  body('bankName')
    .optional()
    .notEmpty()
    .withMessage('bankName must not be empty')
    .isString()
    .withMessage('bankName must be a string'),

  body('bankBranchName')
    .optional()
    .notEmpty()
    .withMessage('bankBranchName must not be empty')
    .isString()
    .withMessage('bankBranchName must be a string'),

  body('bankBranchCode')
    .optional()
    .notEmpty()
    .withMessage('bankBranchCode must not be empty')
    .isString()
    .withMessage('bankBranchCode must be a string'),

  body('bankAccountNumber')
    .optional()
    .notEmpty()
    .withMessage('bankAccountNumber must not be empty')
    .isString()
    .withMessage('bankAccountNumber must be a string'),

  body('bankAccountType')
    .optional()
    .notEmpty()
    .withMessage('bankAccountType must not be empty')
    .isString()
    .withMessage('bankAccountType must be a string'),

  body('bankBranchCode')
    .optional()
    .notEmpty()
    .withMessage('bankBranchCode must not be empty')
    .isString()
    .withMessage('bankBranchCode must be a string'),

  body('bankAccountContactPerson')
    .optional()
    .notEmpty()
    .withMessage('bankAccountContactPerson must not be empty')
    .isString()
    .withMessage('bankAccountContactPerson must be a string'),

  body('bankAuthorizedSignature')
    .optional()
    .notEmpty()
    .withMessage('bankAuthorizedSignature must not be empty')
    .isString()
    .withMessage('bankAuthorizedSignature must be a string'),

  body('bankDate')
    .optional()
    .notEmpty()
    .withMessage('bankDate must not be empty')
    .isISO8601()
    .withMessage(
      'bankDate must be a valid ISO 8601 date 2024-05-23T08:25:23.176Z'
    ),

  body('bankNameInPrint')
    .optional()
    .notEmpty()
    .withMessage('bankNameInPrint must not be empty')
    .isString()
    .withMessage('bankNameInPrint must be a string'),

  body('driverLicenseImg')
    .optional()
    .notEmpty()
    .withMessage('driverLicenseImg must not be empty')
    .isString()
    .withMessage('driverLicenseImg must be a string'),

  body('driverIdentityCardImg')
    .optional()
    .notEmpty()
    .withMessage('driverIdentityCardImg must not be empty')
    .isString()
    .withMessage('driverIdentityCardImg must be a string'),

  body('comprehensiveInsuranceImg')
    .optional()
    .notEmpty()
    .withMessage('comprehensiveInsuranceImg must not be empty')
    .isString()
    .withMessage('comprehensiveInsuranceImg must be a string'),

  body('operatorCardImg')
    .optional()
    .notEmpty()
    .withMessage('operatorCardImg must not be empty')
    .isString()
    .withMessage('operatorCardImg must be a string'),

  body('inspectionCertImg')
    .optional()
    .notEmpty()
    .withMessage('inspectionCertImg must not be empty')
    .isString()
    .withMessage('inspectionCertImg must be a string'),

  body('professionalDriverPermitImg')
    .optional()
    .notEmpty()
    .withMessage('professionalDriverPermitImg must not be empty')
    .isString()
    .withMessage('professionalDriverPermitImg must be a string'),

  body('commissionConsent')
    .optional()
    .notEmpty()
    .withMessage('commissionConsent must not be empty')
    .custom((value) => {
      if (value !== true) {
        throw new Error('commissionConsent must be true');
      }
      return true;
    }),

  body('commissionConsentAuthorizedSignatureImg')
    .optional()
    .notEmpty()
    .withMessage('commissionConsentAuthorizedSignatureImg must not be empty')
    .isString()
    .withMessage('commissionConsentAuthorizedSignatureImg must be a string'),

  body('commissionConsentDate')
    .optional()
    .notEmpty()
    .withMessage('commissionConsentDate must not be empty')
    .isISO8601()
    .withMessage(
      'commissionConsentDate must be a valid ISO 8601 date 2024-05-23T08:25:23.176Z'
    ),

  body('commissionConsentNameInPrint')
    .optional()
    .notEmpty()
    .withMessage('commissionConsentNameInPrint must not be empty')
    .isString()
    .withMessage('commissionConsentNameInPrint must be a string'),

  body('tcConsent')
    .optional()
    .notEmpty()
    .withMessage('tcConsent must not be empty')
    .custom((value) => {
      if (value !== true) {
        throw new Error('tcConsent must be true');
      }
      return true;
    }),

  body('tcFullName')
    .optional()
    .notEmpty()
    .withMessage('tcFullName must not be empty')
    .isString()
    .withMessage('tcFullName must be a string'),

  body('tcAuthorizedSignatureImg')
    .optional()
    .notEmpty()
    .withMessage('tcAuthorizedSignatureImg must not be empty')
    .isString()
    .withMessage('tcAuthorizedSignatureImg must be a string'),

  body('tcDate')
    .optional()
    .notEmpty()
    .withMessage('tcDate must not be empty')
    .isISO8601()
    .withMessage(
      'tcDate must be a valid ISO 8601 date 2024-05-23T08:25:23.176Z'
    ),

  body('tcPlace')
    .optional()
    .notEmpty()
    .withMessage('tcPlace must not be empty')
    .isString()
    .withMessage('tcPlace must be a string'),
];

module.exports = { userUpdateValidation };
