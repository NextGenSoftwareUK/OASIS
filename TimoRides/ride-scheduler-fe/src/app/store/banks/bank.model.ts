export enum COUNTRY_CODE_ENUMS {
  'Egypt' = 'EG',
  'Ethiopia' = 'ET',
  'Ghana' = 'GH',
  'Kenya' = 'KE',
  'Malawi' = 'MW',
  'Nigeria' = 'NG',
  'Rwanda' = 'RW',
  'Sierra Leone' = 'SL',
  'Tanzania' = 'TZ',
  'Uganda' = 'UG',
  'United States' = 'US',
  'South Africa' = 'ZA',
}

export type BankResponse = {
  status: string;
  message: string;
  data: BankInfo[];
};

export type BankInfo = {
  id: number;
  code: string;
  name: string;
};
