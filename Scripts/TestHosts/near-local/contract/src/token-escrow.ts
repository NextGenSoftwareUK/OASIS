import type { RecordMap } from "./checked-record";

export const tokenEscrowPrefix = "token-escrow:";
export interface TokenDeposit {
  protocol: number; lock_id: string; symbol: string; owner_id: string; amount: string; released: boolean;
}
function lockId(value: string): string {
  if(typeof value!=="string"||! /^[0-9a-f]{32}$/.test(value)||/^0+$/.test(value))throw new Error("Canonical nonempty token deposit ID required");
  return value;
}
function symbolName(value: string): string {
  if(typeof value!=="string"||!value.trim()||value.length>256||/[\u0000-\u001f\u007f]/.test(value))throw new Error("Exact bounded token symbol required");
  return value;
}
function ownerId(value: string): string {
  if(typeof value!=="string"||value==="system"||value.length<2||value.length>64||!/^([a-z0-9]+[._-])*[a-z0-9]+$/.test(value))throw new Error("Canonical native owner account required");
  return value;
}
function units(value: string, positive=false): bigint {
  if(typeof value!=="string"||! /^(0|[1-9][0-9]*)$/.test(value)||value.length>78)throw new Error("Canonical bounded token units required");
  const amount=BigInt(value);
  if(amount>=(1n<<256n)||(positive&&amount===0n))throw new Error("Token units outside allowed range");
  return amount;
}
function readToken(tokens: RecordMap, symbol: string): any {
  const raw=tokens.get(symbolName(symbol));if(raw===null)throw new Error("Token does not exist");
  const token=JSON.parse(raw);
  if(!token||token.symbol!==symbol||typeof token.locked!=="boolean"||!token.balances||typeof token.balances!=="object"||Array.isArray(token.balances))throw new Error("Invalid native token state");
  units(token.supply);return token;
}
function balance(token: any, owner: string): bigint {
  const value=units(token.balances[owner]??"0");if(value>units(token.supply))throw new Error("Balance exceeds token supply");return value;
}
export function readTokenDeposit(records: RecordMap, id: string): TokenDeposit|null {
  const raw=records.get(tokenEscrowPrefix+lockId(id));if(raw===null)return null;
  const deposit=JSON.parse(raw);
  const fields=["protocol","lock_id","symbol","owner_id","amount","released"];
  if(!deposit||typeof deposit!=="object"||Array.isArray(deposit)||Object.keys(deposit).length!==fields.length
    ||fields.some(key=>!Object.prototype.hasOwnProperty.call(deposit,key))||deposit.protocol!==1||deposit.lock_id!==id||typeof deposit.released!=="boolean")throw new Error("Invalid native deposit state");
  symbolName(deposit.symbol);ownerId(deposit.owner_id);units(deposit.amount,true);return deposit;
}
export function lockTokenDeposit(tokens: RecordMap, records: RecordMap, owner: string, symbol: string, id: string, amount: string): void {
  ownerId(owner);lockId(id);symbolName(symbol);const quantity=units(amount,true);
  if(records.get(tokenEscrowPrefix+id)!==null)throw new Error("Token deposit ID already used");
  const token=readToken(tokens,symbol);if(token.locked)throw new Error("Token trading is frozen");
  const available=balance(token,owner);if(available<quantity)throw new Error("Insufficient unlocked token balance");
  const deposit:TokenDeposit={protocol:1,lock_id:id,symbol,owner_id:owner,amount,released:false};
  token.balances[owner]=(available-quantity).toString();
  // Both writes are in one native function-call receipt: NEAR owns rollback.
  // Every semantic check precedes either write; no alternate transfer/journal path.
  tokens.set(symbol,JSON.stringify(token));records.set(tokenEscrowPrefix+id,JSON.stringify(deposit));
}
export function releaseTokenDeposit(tokens: RecordMap, records: RecordMap, owner: string, symbol: string, id: string): void {
  ownerId(owner);symbolName(symbol);const deposit=readTokenDeposit(records,id);
  if(deposit===null||deposit.symbol!==symbol||deposit.owner_id!==owner||deposit.released)throw new Error("Unreleased original owned token deposit required");
  const token=readToken(tokens,symbol),quantity=units(deposit.amount,true),available=balance(token,owner);
  if(available+quantity>units(token.supply))throw new Error("Deposit exceeds remaining token supply");
  token.balances[owner]=(available+quantity).toString();deposit.released=true;
  // Release returns custody to the same owner even while trading is frozen.
  // Keep the released tombstone so its original ID can never lock/release again.
  tokens.set(symbol,JSON.stringify(token));records.set(tokenEscrowPrefix+id,JSON.stringify(deposit));
}
export function burnSpendableToken(tokens: RecordMap, owner: string, symbol: string, amount: string): void {
  ownerId(owner);const token=readToken(tokens,symbol),quantity=units(amount,true),available=balance(token,owner);
  if(token.locked||available<quantity)throw new Error("Positive burn within unfrozen unlocked balance required");
  token.balances[owner]=(available-quantity).toString();
  // Supply includes escrowed balances. Summing spendable balances would destroy custody value.
  token.supply=(units(token.supply)-quantity).toString();tokens.set(symbol,JSON.stringify(token));
}
export function requireApplicationRecordKey(key: string): void {
  if(typeof key!=="string"||!key||key.startsWith(tokenEscrowPrefix))throw new Error("Reserved token custody records cannot be written through generic storage");
}
