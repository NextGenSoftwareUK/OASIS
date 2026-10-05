export const generaterandomNumber = () => {
  const randomNumber = Math.random();
  const strRandomNumber = randomNumber.toString().substring(2, 10);
  return strRandomNumber;
};
