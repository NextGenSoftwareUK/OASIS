export function tokenUnits(amount, decimals) {
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 255 || typeof amount !== 'string' || !/^[0-9]+(?:\.[0-9]+)?$/.test(amount))
    throw new Error('Exact positive decimal token amount and ledger decimals are required.');
  const [whole, fractional = ''] = amount.split('.');
  if (fractional.length > decimals && /[1-9]/.test(fractional.slice(decimals)))
    throw new Error('Token amount exceeds ledger decimal precision.');
  const units = BigInt(whole + fractional.slice(0, decimals).padEnd(decimals, '0'));
  if (units <= 0n || units > 9223372036854775807n) throw new Error('Token units must fit positive int64.');
  return units;
}
