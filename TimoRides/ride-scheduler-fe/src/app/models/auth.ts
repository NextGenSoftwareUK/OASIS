export type UserRegistration = {
  email: string;
  password: string;
  userType: string;
  fullName: string;
  phone: string;
};

export type UserRegistrationResponse = {
  message: string;
};

export type UserLogin = {
  email: string;
  password: string;
};

export type UserLoginResponse = InvalidUser | LoggedInUser;

export type InvalidUser = {
  error: string;
};

export type LoggedInUser = {
  fullName: string;
  phone: string;
  email: string;
  isVerified: boolean;
  isDisable: boolean;
  role: string;
  accessToken: string;
  refreshToken: string;
};

export type EmailVerificationRequest = {
  token: string;
};

export type EmailVerificationResponse = {
  message: string;
};

export type EmailVerificationLinkRequest = {
  email: string;
};

export type EmailVerificationLinkResponse = {
  message: string;
};

export type RefreshTokenLinkRequest = {
  refreshToken: string;
};

export type RefreshTokenLinkResponse = {
  accessToken: string;
  refreshToken: string;
};
