use cosmwasm_std::{to_json_binary, Addr, Binary, Deps, DepsMut, MessageInfo, Response, StdError, StdResult, Uint128};
use cw_storage_plus::Map;
use schemars::JsonSchema;
use serde::{Deserialize, Serialize};

// OASIS custom assets, not a claim of CW20/CW721 interoperability. Ledger state,
// balances and ownership live in CosmWasm and every mutation is signed by its actor.
const TOKENS: Map<&str, Token> = Map::new("asset_tokens");
const BALANCES: Map<(&str, &Addr), Uint128> = Map::new("asset_balances");
const NFTS: Map<&str, Nft> = Map::new("asset_nfts");
const TOKEN_DEPOSITS: Map<&str, TokenDeposit> = Map::new("asset_token_deposits_v1");

// A deposit removes spendable balance without changing supply. Released records
// remain permanently occupied so an original operation ID cannot be replayed.
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct TokenDeposit {
    pub deposit_id: String, pub symbol: String, pub owner: Addr,
    pub amount: Uint128, pub released: bool,
}

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct Token { pub symbol: String, pub issuer: Addr, pub supply: Uint128, pub locked: bool, pub metadata_json: String }

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct Nft { pub token_id: String, pub owner: Addr, pub locked: bool, pub metadata_json: String }

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum ExecuteMsg {
    TokenMint { symbol: String, recipient: String, amount: Uint128, metadata_json: String },
    TokenTransfer { symbol: String, recipient: String, amount: Uint128 },
    TokenBurn { symbol: String, amount: Uint128 },
    TokenSetLocked { symbol: String, locked: bool },
    TokenDeposit { deposit_id: String, symbol: String, amount: Uint128 },
    TokenRelease { deposit_id: String, symbol: String, amount: Uint128 },
    NftMint { token_id: String, recipient: String, metadata_json: String },
    NftTransfer { token_id: String, recipient: String },
    NftBurn { token_id: String },
    NftSetLocked { token_id: String, locked: bool },
}

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum QueryMsg {
    Token { symbol: String },
    TokenBalance { symbol: String, address: String },
    TokenDeposit { deposit_id: String },
    TokenEscrowProtocol {},
    Nft { token_id: String },
}

fn require_admin(deps: Deps, sender: &Addr) -> StdResult<()> {
    if *sender != super::OWNER.load(deps.storage)? {
        return Err(StdError::generic_err("unauthorized: asset minter required"));
    }
    Ok(())
}

fn unlocked_token(deps: Deps, symbol: &str) -> StdResult<Token> {
    let token = TOKENS.load(deps.storage, symbol)?;
    if token.locked { return Err(StdError::generic_err("token is locked")); }
    Ok(token)
}

fn owned_nft(deps: Deps, token_id: &str, sender: &Addr) -> StdResult<Nft> {
    let nft = NFTS.load(deps.storage, token_id)?;
    if nft.owner != *sender { return Err(StdError::generic_err("unauthorized: NFT owner required")); }
    Ok(nft)
}

fn validate_deposit_id(id: &str) -> StdResult<()> {
    if id.len() != 64 || !id.bytes().all(|b| b.is_ascii_digit() || (b'a'..=b'f').contains(&b)) {
        return Err(StdError::generic_err("canonical lowercase SHA256 deposit ID required"));
    }
    Ok(())
}

pub fn execute(deps: DepsMut, info: MessageInfo, msg: ExecuteMsg) -> StdResult<Response> {
    if !info.funds.is_empty() { return Err(StdError::generic_err("asset calls do not accept native funds")); }
    let action = match msg {
        ExecuteMsg::TokenMint { symbol, recipient, amount, metadata_json } => {
            require_admin(deps.as_ref(), &info.sender)?;
            if symbol.is_empty() || amount.is_zero() { return Err(StdError::generic_err("symbol and positive amount required")); }
            let recipient = deps.api.addr_validate(&recipient)?;
            let mut token = TOKENS.may_load(deps.storage, &symbol)?.unwrap_or(Token {
                symbol: symbol.clone(), issuer: info.sender.clone(), supply: Uint128::zero(), locked: false, metadata_json,
            });
            if token.issuer != info.sender { return Err(StdError::generic_err("unauthorized: token issuer required")); }
            if token.locked { return Err(StdError::generic_err("token is locked")); }
            token.supply = token.supply.checked_add(amount)?;
            let balance = BALANCES.may_load(deps.storage, (&symbol, &recipient))?.unwrap_or_default().checked_add(amount)?;
            TOKENS.save(deps.storage, &symbol, &token)?;
            BALANCES.save(deps.storage, (&symbol, &recipient), &balance)?;
            "token_mint"
        }
        ExecuteMsg::TokenTransfer { symbol, recipient, amount } => {
            unlocked_token(deps.as_ref(), &symbol)?;
            if amount.is_zero() { return Err(StdError::generic_err("positive amount required")); }
            let recipient = deps.api.addr_validate(&recipient)?;
            let sender_balance = BALANCES.may_load(deps.storage, (&symbol, &info.sender))?.unwrap_or_default();
            let remaining = sender_balance.checked_sub(amount)?;
            // Self-transfers still validate sufficient funds, then leave the balance unchanged.
            if recipient != info.sender {
                let received = BALANCES.may_load(deps.storage, (&symbol, &recipient))?.unwrap_or_default().checked_add(amount)?;
                BALANCES.save(deps.storage, (&symbol, &info.sender), &remaining)?;
                BALANCES.save(deps.storage, (&symbol, &recipient), &received)?;
            }
            "token_transfer"
        }
        ExecuteMsg::TokenBurn { symbol, amount } => {
            let mut token = unlocked_token(deps.as_ref(), &symbol)?;
            let balance = BALANCES.may_load(deps.storage, (&symbol, &info.sender))?.unwrap_or_default();
            if amount.is_zero() { return Err(StdError::generic_err("positive burn amount required")); }
            let remaining = balance.checked_sub(amount)?;
            token.supply = token.supply.checked_sub(amount)?;
            TOKENS.save(deps.storage, &symbol, &token)?;
            if remaining.is_zero() { BALANCES.remove(deps.storage, (&symbol, &info.sender)); }
            else { BALANCES.save(deps.storage, (&symbol, &info.sender), &remaining)?; }
            "token_burn"
        }
        ExecuteMsg::TokenDeposit { deposit_id, symbol, amount } => {
            validate_deposit_id(&deposit_id)?;
            unlocked_token(deps.as_ref(), &symbol)?;
            if amount.is_zero() { return Err(StdError::generic_err("positive deposit amount required")); }
            if TOKEN_DEPOSITS.has(deps.storage, &deposit_id) {
                return Err(StdError::generic_err("deposit ID already used"));
            }
            let balance = BALANCES.may_load(deps.storage, (&symbol, &info.sender))?.unwrap_or_default();
            let remaining = balance.checked_sub(amount)?;
            TOKEN_DEPOSITS.save(deps.storage, &deposit_id, &TokenDeposit {
                deposit_id: deposit_id.clone(), symbol: symbol.clone(), owner: info.sender.clone(), amount, released: false,
            })?;
            BALANCES.save(deps.storage, (&symbol, &info.sender), &remaining)?;
            "token_deposit"
        }
        ExecuteMsg::TokenRelease { deposit_id, symbol, amount } => {
            validate_deposit_id(&deposit_id)?;
            let mut deposit = TOKEN_DEPOSITS.load(deps.storage, &deposit_id)?;
            if deposit.owner != info.sender || deposit.symbol != symbol || deposit.amount != amount || deposit.released {
                return Err(StdError::generic_err("active owned deposit with exact symbol and amount required"));
            }
            // Issuer freeze affects new spending, not return of an existing deposit.
            let balance = BALANCES.may_load(deps.storage, (&symbol, &info.sender))?.unwrap_or_default().checked_add(amount)?;
            deposit.released = true;
            TOKEN_DEPOSITS.save(deps.storage, &deposit_id, &deposit)?;
            BALANCES.save(deps.storage, (&symbol, &info.sender), &balance)?;
            "token_release"
        }
        ExecuteMsg::TokenSetLocked { symbol, locked } => {
            let mut token = TOKENS.load(deps.storage, &symbol)?;
            if token.issuer != info.sender { return Err(StdError::generic_err("unauthorized: token issuer required")); }
            token.locked = locked;
            TOKENS.save(deps.storage, &symbol, &token)?;
            "token_set_locked"
        }
        ExecuteMsg::NftMint { token_id, recipient, metadata_json } => {
            require_admin(deps.as_ref(), &info.sender)?;
            if token_id.is_empty() { return Err(StdError::generic_err("token ID required")); }
            if NFTS.has(deps.storage, &token_id) { return Err(StdError::generic_err("NFT already exists")); }
            let owner = deps.api.addr_validate(&recipient)?;
            NFTS.save(deps.storage, &token_id, &Nft { token_id: token_id.clone(), owner, locked: false, metadata_json })?;
            "nft_mint"
        }
        ExecuteMsg::NftTransfer { token_id, recipient } => {
            let mut nft = owned_nft(deps.as_ref(), &token_id, &info.sender)?;
            if nft.locked { return Err(StdError::generic_err("NFT is locked")); }
            nft.owner = deps.api.addr_validate(&recipient)?;
            NFTS.save(deps.storage, &token_id, &nft)?;
            "nft_transfer"
        }
        ExecuteMsg::NftBurn { token_id } => {
            let nft = owned_nft(deps.as_ref(), &token_id, &info.sender)?;
            if nft.locked { return Err(StdError::generic_err("NFT is locked")); }
            NFTS.remove(deps.storage, &token_id);
            "nft_burn"
        }
        ExecuteMsg::NftSetLocked { token_id, locked } => {
            let mut nft = owned_nft(deps.as_ref(), &token_id, &info.sender)?;
            nft.locked = locked;
            NFTS.save(deps.storage, &token_id, &nft)?;
            "nft_set_locked"
        }
    };
    Ok(Response::new().add_attribute("action", action))
}

pub fn query(deps: Deps, msg: QueryMsg) -> StdResult<Binary> {
    match msg {
        QueryMsg::TokenEscrowProtocol {} => to_json_binary(&1u32),
        QueryMsg::TokenDeposit { deposit_id } => {
            validate_deposit_id(&deposit_id)?;
            to_json_binary(&TOKEN_DEPOSITS.may_load(deps.storage, &deposit_id)?)
        }
        QueryMsg::Token { symbol } => to_json_binary(&TOKENS.load(deps.storage, &symbol)?),
        QueryMsg::TokenBalance { symbol, address } => {
            TOKENS.load(deps.storage, &symbol)?;
            let address = deps.api.addr_validate(&address)?;
            to_json_binary(&BALANCES.may_load(deps.storage, (&symbol, &address))?.unwrap_or_default())
        }
        QueryMsg::Nft { token_id } => to_json_binary(&NFTS.load(deps.storage, &token_id)?),
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use cosmwasm_std::{from_json, testing::{mock_dependencies, mock_env, message_info}};

    #[test]
    fn deposits_hold_exact_amounts_and_release_only_the_original_owned_record() {
        let mut deps = mock_dependencies();
        let issuer = deps.api.addr_make("issuer");
        let owner = deps.api.addr_make("holder");
        let other = deps.api.addr_make("other");
        super::super::instantiate(deps.as_mut(), mock_env(), message_info(&issuer, &[]), super::super::InstantiateMsg { owner: None }).unwrap();
        execute(deps.as_mut(), message_info(&issuer, &[]), ExecuteMsg::TokenMint {
            symbol: "TREE".into(), recipient: owner.to_string(), amount: Uint128::new(100), metadata_json: "{}".into(),
        }).unwrap();
        let id = "a".repeat(64);
        let second = "b".repeat(64);
        let deposit = ExecuteMsg::TokenDeposit { deposit_id: id.clone(), symbol: "TREE".into(), amount: Uint128::new(30) };
        execute(deps.as_mut(), message_info(&owner, &[]), deposit.clone()).unwrap();
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), deposit.clone()).is_err());
        assert!(execute(deps.as_mut(), message_info(&other, &[]), deposit.clone()).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenDeposit {
            deposit_id: second.clone(), symbol: "TREE".into(), amount: Uint128::new(20),
        }).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &owner)).unwrap(), Uint128::new(50));
        assert_eq!(TOKENS.load(deps.as_ref().storage, "TREE").unwrap().supply, Uint128::new(100));
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenTransfer {
            symbol: "TREE".into(), recipient: other.to_string(), amount: Uint128::new(51),
        }).is_err());
        let release = ExecuteMsg::TokenRelease { deposit_id: id.clone(), symbol: "TREE".into(), amount: Uint128::new(30) };
        assert!(execute(deps.as_mut(), message_info(&other, &[]), release.clone()).is_err());
        assert!(execute(deps.as_mut(), message_info(&issuer, &[]), release.clone()).is_err());
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenRelease {
            deposit_id: id.clone(), symbol: "WRONG".into(), amount: Uint128::new(30),
        }).is_err());
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenRelease {
            deposit_id: id.clone(), symbol: "TREE".into(), amount: Uint128::new(29),
        }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), release.clone()).unwrap();
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), release).is_err());
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), deposit).is_err());
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &owner)).unwrap(), Uint128::new(80));
        let original: Option<TokenDeposit> = from_json(query(deps.as_ref(), QueryMsg::TokenDeposit { deposit_id: id }).unwrap()).unwrap();
        assert!(original.unwrap().released);
        assert!(!TOKEN_DEPOSITS.load(deps.as_ref().storage, &second).unwrap().released);
        assert_eq!(TOKENS.load(deps.as_ref().storage, "TREE").unwrap().supply, Uint128::new(100));
    }

    #[test]
    fn deposit_preconditions_reject_without_balance_changes_and_freeze_cannot_trap_release() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        super::super::instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), super::super::InstantiateMsg { owner: None }).unwrap();
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenMint {
            symbol: "TREE".into(), recipient: owner.to_string(), amount: Uint128::new(100), metadata_json: "{}".into(),
        }).unwrap();
        for (id, symbol, amount, funds) in [
            ("short".into(), "TREE", 1, vec![]), ("A".repeat(64), "TREE", 1, vec![]),
            ("a".repeat(64), "TREE", 0, vec![]), ("a".repeat(64), "TREE", 101, vec![]),
            ("a".repeat(64), "missing", 1, vec![]),
            ("a".repeat(64), "TREE", 1, cosmwasm_std::coins(1, "utest")),
        ] {
            assert!(execute(deps.as_mut(), message_info(&owner, &funds), ExecuteMsg::TokenDeposit {
                deposit_id: id, symbol: symbol.into(), amount: Uint128::new(amount),
            }).is_err());
            assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &owner)).unwrap(), Uint128::new(100));
        }
        let id = "c".repeat(64);
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenDeposit {
            deposit_id: id.clone(), symbol: "TREE".into(), amount: Uint128::new(100),
        }).unwrap();
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenSetLocked { symbol: "TREE".into(), locked: true }).unwrap();
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenDeposit {
            deposit_id: "d".repeat(64), symbol: "TREE".into(), amount: Uint128::new(1),
        }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenRelease {
            deposit_id: id, symbol: "TREE".into(), amount: Uint128::new(100),
        }).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &owner)).unwrap(), Uint128::new(100));
        let protocol: u32 = from_json(query(deps.as_ref(), QueryMsg::TokenEscrowProtocol {}).unwrap()).unwrap();
        assert_eq!(protocol, 1);
        let missing: Option<TokenDeposit> = from_json(query(deps.as_ref(), QueryMsg::TokenDeposit { deposit_id: "d".repeat(64) }).unwrap()).unwrap();
        assert_eq!(missing, None);
    }

    #[test]
    fn maximum_uint128_deposit_roundtrips_without_supply_or_precision_loss() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        super::super::instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), super::super::InstantiateMsg { owner: None }).unwrap();
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenMint {
            symbol: "MAX".into(), recipient: owner.to_string(), amount: Uint128::MAX, metadata_json: "{}".into(),
        }).unwrap();
        let id = "f".repeat(64);
        // Use the public wrapped entry points and actual JSON wire representation.
        let deposit: super::super::ExecuteMsg = from_json(format!(
            r#"{{"asset":{{"message":{{"token_deposit":{{"deposit_id":"{}","symbol":"MAX","amount":"{}"}}}}}}}}"#,
            id, Uint128::MAX,
        ).as_bytes()).unwrap();
        super::super::execute(deps.as_mut(), mock_env(), message_info(&owner, &[]), deposit).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("MAX", &owner)).unwrap(), Uint128::zero());
        let saved: Option<TokenDeposit> = from_json(super::super::query(deps.as_ref(), mock_env(),
            super::super::QueryMsg::Asset { message: QueryMsg::TokenDeposit { deposit_id: id.clone() } }).unwrap()).unwrap();
        assert_eq!(saved.unwrap().amount, Uint128::MAX);
        super::super::execute(deps.as_mut(), mock_env(), message_info(&owner, &[]),
            super::super::ExecuteMsg::Asset { message: ExecuteMsg::TokenRelease {
                deposit_id: id, symbol: "MAX".into(), amount: Uint128::MAX,
            } }).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("MAX", &owner)).unwrap(), Uint128::MAX);
        assert_eq!(TOKENS.load(deps.as_ref().storage, "MAX").unwrap().supply, Uint128::MAX);
    }

    #[test]
    fn token_supply_balances_locks_and_actor_authority() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        let other = deps.api.addr_make("other");
        super::super::instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), super::super::InstantiateMsg { owner: None }).unwrap();
        let mint = ExecuteMsg::TokenMint { symbol: "TREE".into(), recipient: owner.to_string(), amount: Uint128::new(100), metadata_json: "{}".into() };
        assert!(execute(deps.as_mut(), message_info(&other, &[]), mint.clone()).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), mint).unwrap();
        let transfer = ExecuteMsg::TokenTransfer { symbol: "TREE".into(), recipient: other.to_string(), amount: Uint128::new(30) };
        assert!(execute(deps.as_mut(), message_info(&other, &[]), transfer.clone()).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), transfer).unwrap();
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenTransfer { symbol: "TREE".into(), recipient: owner.to_string(), amount: Uint128::new(5) }).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &owner)).unwrap(), Uint128::new(70));
        assert!(execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenSetLocked { symbol: "TREE".into(), locked: true }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenSetLocked { symbol: "TREE".into(), locked: true }).unwrap();
        assert!(execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenBurn { symbol: "TREE".into(), amount: Uint128::new(1) }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::TokenSetLocked { symbol: "TREE".into(), locked: false }).unwrap();
        execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenBurn { symbol: "TREE".into(), amount: Uint128::new(1) }).unwrap();
        assert_eq!(BALANCES.load(deps.as_ref().storage, ("TREE", &other)).unwrap(), Uint128::new(29));
        assert_eq!(TOKENS.load(deps.as_ref().storage, "TREE").unwrap().supply, Uint128::new(99));
        assert!(execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenBurn { symbol: "TREE".into(), amount: Uint128::zero() }).is_err());
        assert!(execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenBurn { symbol: "TREE".into(), amount: Uint128::new(30) }).is_err());
        execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::TokenBurn { symbol: "TREE".into(), amount: Uint128::new(29) }).unwrap();
        let balance: Uint128 = from_json(query(deps.as_ref(), QueryMsg::TokenBalance { symbol: "TREE".into(), address: other.to_string() }).unwrap()).unwrap();
        assert_eq!(balance, Uint128::zero());
        assert_eq!(TOKENS.load(deps.as_ref().storage, "TREE").unwrap().supply, Uint128::new(70));
    }

    #[test]
    fn nft_mint_owner_transfer_lock_burn_and_duplicate_rejection() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        let other = deps.api.addr_make("other");
        super::super::instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), super::super::InstantiateMsg { owner: None }).unwrap();
        let mint = ExecuteMsg::NftMint { token_id: "oak".into(), recipient: owner.to_string(), metadata_json: "{\"title\":\"Oak\"}".into() };
        assert!(execute(deps.as_mut(), message_info(&other, &[]), mint.clone()).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), mint.clone()).unwrap();
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), mint).is_err());
        assert!(execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::NftBurn { token_id: "oak".into() }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::NftSetLocked { token_id: "oak".into(), locked: true }).unwrap();
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::NftTransfer { token_id: "oak".into(), recipient: other.to_string() }).is_err());
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::NftSetLocked { token_id: "oak".into(), locked: false }).unwrap();
        execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::NftTransfer { token_id: "oak".into(), recipient: other.to_string() }).unwrap();
        let nft: Nft = from_json(query(deps.as_ref(), QueryMsg::Nft { token_id: "oak".into() }).unwrap()).unwrap();
        assert_eq!(nft.owner, other);
        assert!(execute(deps.as_mut(), message_info(&owner, &[]), ExecuteMsg::NftBurn { token_id: "oak".into() }).is_err());
        execute(deps.as_mut(), message_info(&other, &[]), ExecuteMsg::NftBurn { token_id: "oak".into() }).unwrap();
        assert!(query(deps.as_ref(), QueryMsg::Nft { token_id: "oak".into() }).is_err());
    }
}
