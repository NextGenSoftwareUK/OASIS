export type PaymentCard = {
  cardNumber: string;
  cardholderName: string;
  cardExpiration: string;
  ccv: string;
  isCardSaved: boolean;
  cardType: CardType;
};

export type CardType = {
  id: number;
  cardName: string;
  altName: string;
  cardImagepath: string;
};

export type ICardType = {
  cardTypes: CardType[];
};

export type PaymentOption = {
  payOption: 'optCash' | 'optCard';
};
