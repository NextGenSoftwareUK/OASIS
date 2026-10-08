import { NearBindgen, call, initialize, near, view, UnorderedMap } from "near-sdk-js";

@NearBindgen({ requireInit: true })
export class OasisStorage {
  owner_id: string = "";
  records: UnorderedMap<string> = new UnorderedMap<string>("records");
  nfts: UnorderedMap<string> = new UnorderedMap<string>("nfts");
  tokens: UnorderedMap<string> = new UnorderedMap<string>("tokens");

  @initialize({})
  init({ owner_id }: { owner_id: string }): void {
    if (!owner_id) throw new Error("owner_id is required");
    this.owner_id = owner_id;
  }

  private requireContractOwner(): void {
    if (near.predecessorAccountId() !== this.owner_id) throw new Error("Only the OASIS contract owner may perform this operation");
  }

  @call({})
  put({ key, value }: { key: string; value: string }): void {
    this.requireContractOwner();
    this.records.set(key, value);
  }

  @call({})
  delete_record({ key }: { key: string }): void {
    this.requireContractOwner();
    this.records.remove(key);
  }

  @view({})
  get({ key }: { key: string }): string | null {
    return this.records.get(key);
  }

  @view({})
  entries({ prefix = "" }: { prefix?: string }): Array<[string, string]> {
    return this.records.toArray().filter(([key]) => key.startsWith(prefix));
  }

  @call({})
  nft_mint({ token_id, owner_id, metadata_json }: { token_id: string; owner_id: string; metadata_json: string }): void {
    this.requireContractOwner();
    if (this.nfts.get(token_id) !== null) throw new Error(`NFT ${token_id} already exists`);
    this.nfts.set(token_id, JSON.stringify({ token_id, owner_id, metadata_json, locked: false }));
  }

  @view({})
  nft_token({ token_id }: { token_id: string }): object | null {
    const value = this.nfts.get(token_id);
    return value === null ? null : JSON.parse(value);
  }

  @call({})
  nft_transfer({ token_id, receiver_id }: { token_id: string; receiver_id: string }): void {
    const value = this.nfts.get(token_id);
    if (value === null) throw new Error(`NFT ${token_id} does not exist`);
    const nft = JSON.parse(value);
    if (nft.locked) throw new Error(`NFT ${token_id} is locked`);
    if (near.predecessorAccountId() !== nft.owner_id) throw new Error(`Only the owner may transfer NFT ${token_id}`);
    nft.owner_id = receiver_id;
    this.nfts.set(token_id, JSON.stringify(nft));
  }

  @call({})
  nft_burn({ token_id }: { token_id: string }): void {
    const value = this.nfts.get(token_id);
    if (value === null) throw new Error(`NFT ${token_id} does not exist`);
    const nft = JSON.parse(value);
    if (near.predecessorAccountId() !== nft.owner_id) throw new Error(`Only the owner may burn NFT ${token_id}`);
    this.nfts.remove(token_id);
  }

  @call({})
  nft_set_locked({ token_id, locked }: { token_id: string; locked: boolean }): void {
    const value = this.nfts.get(token_id);
    if (value === null) throw new Error(`NFT ${token_id} does not exist`);
    const nft = JSON.parse(value);
    if (near.predecessorAccountId() !== nft.owner_id) throw new Error(`Only the owner may change the lock for NFT ${token_id}`);
    nft.locked = locked;
    this.nfts.set(token_id, JSON.stringify(nft));
  }

  @call({})
  token_mint({ symbol, owner_id, amount, metadata_json }: { symbol: string; owner_id: string; amount: string; metadata_json: string }): void {
    this.requireContractOwner();
    if (this.tokens.get(symbol) !== null) throw new Error(`Token ${symbol} already exists`);
    this.tokens.set(symbol, JSON.stringify({ symbol, issuer_id: near.predecessorAccountId(), metadata_json, locked: false, balances: { [owner_id]: amount } }));
  }

  @view({})
  token_get({ symbol }: { symbol: string }): object | null {
    const value = this.tokens.get(symbol);
    return value === null ? null : JSON.parse(value);
  }

  @view({})
  token_balance({ symbol, account_id }: { symbol: string; account_id: string }): string {
    const value = this.tokens.get(symbol);
    if (value === null) return "0";
    const token = JSON.parse(value);
    return token.balances[account_id] ?? "0";
  }

  @call({})
  token_transfer({ symbol, from_id, receiver_id, amount }: { symbol: string; from_id: string; receiver_id: string; amount: string }): void {
    const value = this.tokens.get(symbol);
    if (value === null) throw new Error(`Token ${symbol} does not exist`);
    const token = JSON.parse(value);
    if (token.locked) throw new Error(`Token ${symbol} is locked`);
    if (near.predecessorAccountId() !== from_id) throw new Error(`Only ${from_id} may transfer its token balance`);
    const quantity = BigInt(amount);
    const fromBalance = BigInt(token.balances[from_id] ?? "0");
    if (quantity <= 0n || fromBalance < quantity) throw new Error("Insufficient token balance");
    token.balances[from_id] = (fromBalance - quantity).toString();
    token.balances[receiver_id] = (BigInt(token.balances[receiver_id] ?? "0") + quantity).toString();
    this.tokens.set(symbol, JSON.stringify(token));
  }

  @call({})
  token_set_locked({ symbol, locked }: { symbol: string; locked: boolean }): void {
    const value = this.tokens.get(symbol);
    if (value === null) throw new Error(`Token ${symbol} does not exist`);
    const token = JSON.parse(value);
    if (near.predecessorAccountId() !== token.issuer_id) throw new Error(`Only the issuer may change the lock for token ${symbol}`);
    token.locked = locked;
    this.tokens.set(symbol, JSON.stringify(token));
  }

  @call({})
  token_burn({ symbol }: { symbol: string }): void {
    const value = this.tokens.get(symbol);
    if (value === null) throw new Error(`Token ${symbol} does not exist`);
    const token = JSON.parse(value);
    if (near.predecessorAccountId() !== token.issuer_id) throw new Error(`Only the issuer may burn token ${symbol}`);
    this.tokens.remove(symbol);
  }
}
