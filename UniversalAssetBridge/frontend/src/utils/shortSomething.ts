export const shortAddress = (address: string) => {
  if (address) {
    return `${address.slice(0, 5)}...${address.slice(-4)}`;
  }
};

export const shortDescription = (
  description: string | number,
  symbols?: number
) => {
  const descriptionString =
    typeof description === "string" ? description : description.toString();
  if (symbols && descriptionString.length <= symbols) {
    return descriptionString;
  }
  return `${descriptionString.slice(0, symbols || 30)}...`;
};
