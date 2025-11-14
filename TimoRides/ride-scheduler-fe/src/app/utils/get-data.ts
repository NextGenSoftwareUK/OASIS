import { LoggedInUser } from '../models/auth';

export const getSessionToken = (): string => {
  const userInfo = sessionStorage.getItem('userInfo');

  if (userInfo) {
    const userData = JSON.parse(userInfo) as LoggedInUser;
    const requestToken = userData.accessToken;

    return requestToken;
  } else {
    return '';
  }
};
