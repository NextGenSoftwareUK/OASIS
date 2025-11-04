export const calculatePercentageDifference = (
  oldPrice: number,
  newPrice: number
) => {
  if (!oldPrice || oldPrice === 0) return "0%";
  const difference = ((newPrice - oldPrice) / oldPrice) * 100;
  return `${Math.abs(difference).toFixed(2)}%`;
};
