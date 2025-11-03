export const numberRegex: RegExp = /^-?\d*\.?\d*$/;

export const isNumber = (value: string) => {
  return value === 'e' || value === '-';
};
