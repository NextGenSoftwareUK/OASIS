export const rideOptions = [
  {
    id: 1,
    name: 'Timo Go (passengers)',
    value: 'timo-go',
  },

  {
    id: 2,
    name: 'Timo Load (goods)',
    value: 'timo-load',
  },
];

export const profileImageUploadProgress: ProfileImageUploadProgress = {
  status: 'none',
  message: '',
};

export type ProfileImageUploadProgress = {
  status: 'failed' | 'success' | 'pending' | 'none';
  message: string;
};
