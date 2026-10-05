export interface UserProfile {
  accessToken?: string;
  refreshToken?: string;
  id: string;
  fullName: string;
  email: string;
  phone: string;
  profileImg: string;
  homeAddress: string;
  role: UserRole;
  nextOfKinFullName: string;
  nextOfKinEmailAddress: string;
  nextOfKinRelationship: string;
  nextOfKinPhoneCell: string;
  nextOfKinPhoneHome: string;
  nextOfKinHomeAddress: string;
  isDisable: boolean;
  isVerified: boolean;
  wallet: number;
}

export interface DriverProfile extends UserProfile {
  homePhone: string;
  identityType: string;
  nextOfKinFullNameAlt: string;
  nextOfKinEmailAddressAlt: string;
  nextOfKinRelationshipAlt: string;
  nextOfKinPhoneCellAlt: string;
  nextOfKinPhoneHomeAlt: string;
  nextOfKinHomeAddressAlt: string;
  nameOfAccountHolder: string;
  nameOfAccountContactPerson: string;
  bankName: string;
  bankBranchName: string;
  bankBranchCode: string;
  bankAccountNumber: string;
  bankAccountType: string;
  bankAccountContactPerson: string;
  bankAuthorizedSignature: string;
  bankDate: string;
  bankNameInPrint: string;
  driverLicenseImg: string;
  driverIdentityCardImg: string;
  comprehensiveInsuranceImg: string;
  operatorCardImg: string;
  inspectionCertImg: string;
  professionalDriverPermitImg: string;
  commissionConsent: boolean;
  commissionConsentAuthorizedSignatureImg: string;
  commissionConsentDate: string;
  commissionConsentNameInPrint: string;
  tcConsent: boolean;
  tcFullName: string;
  tcAuthorizedSignatureImg: string;
  tcDate: string;
  tcPlace: string;
  title: string;
  identityNumber: string;
  type: string;
  workPhone: string;
  completedRides: number;
  id: string;
}

export type UserProfileResponse = {
  data: UserProfile | DriverProfile;
};

export type UsersProfileResponse = {
  data: UserProfile[] | DriverProfile[];
};

export type Profile = UserProfile | DriverProfile;

export type UserRole = USER_ROLE.USER | USER_ROLE.DRIVER | USER_ROLE.ADMIN;

export enum USER_ROLE {
  USER = 'user',
  DRIVER = 'driver',
  ADMIN = 'admin',
}

export interface LoginResponse extends UserProfile {
  accessToken: string;
  refreshToken: string;
}

export type UserUpdateRequest = {
  [key in keyof UserProfile]: string;
};

export interface UserUpdateResponse extends UserProfile {
  updatedAt: string;
}
