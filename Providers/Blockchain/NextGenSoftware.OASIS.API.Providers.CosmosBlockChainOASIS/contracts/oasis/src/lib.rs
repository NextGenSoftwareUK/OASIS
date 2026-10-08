use cosmwasm_std::{entry_point, to_json_binary, Addr, Binary, Deps, DepsMut, Env, MessageInfo, Order, Response, StdError, StdResult};
use cw_storage_plus::{Bound, Item, Map};
use schemars::JsonSchema;
use serde::{Deserialize, Serialize};
mod assets;

// The shared OASIS key-value provider serializes entities and indexes as opaque
// JSON. Only the configured service signer may mutate this contract's store.
const OWNER: Item<Addr> = Item::new("owner");
const VALUES: Map<&str, String> = Map::new("values");
const MAX_PAGE: u32 = 100;

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct InstantiateMsg { pub owner: Option<String> }

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum ExecuteMsg {
    Put { key: String, value: String },
    Delete { key: String },
    Asset { message: assets::ExecuteMsg },
}

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum QueryMsg {
    Config {},
    Get { key: String },
    List { prefix: String, start_after: Option<String>, limit: Option<u32> },
    Asset { message: assets::QueryMsg },
}

#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct ConfigResponse { pub owner: String }

#[entry_point]
pub fn instantiate(deps: DepsMut, _env: Env, info: MessageInfo, msg: InstantiateMsg) -> StdResult<Response> {
    let owner = match msg.owner {
        Some(owner) => deps.api.addr_validate(&owner)?,
        None => info.sender,
    };
    OWNER.save(deps.storage, &owner)?;
    Ok(Response::new().add_attribute("action", "instantiate").add_attribute("owner", owner))
}

#[entry_point]
pub fn execute(deps: DepsMut, _env: Env, info: MessageInfo, msg: ExecuteMsg) -> StdResult<Response> {
    if let ExecuteMsg::Asset { message } = msg {
        return assets::execute(deps, info, message);
    }
    if info.sender != OWNER.load(deps.storage)? {
        return Err(StdError::generic_err("unauthorized: storage owner required"));
    }
    if !info.funds.is_empty() {
        return Err(StdError::generic_err("storage writes do not accept funds"));
    }
    match msg {
        ExecuteMsg::Put { key, value } => {
            if key.is_empty() { return Err(StdError::generic_err("key must not be empty")); }
            VALUES.save(deps.storage, &key, &value)?;
            Ok(Response::new().add_attribute("action", "put").add_attribute("key", key))
        }
        ExecuteMsg::Delete { key } => {
            VALUES.remove(deps.storage, &key);
            Ok(Response::new().add_attribute("action", "delete").add_attribute("key", key))
        }
        ExecuteMsg::Asset { .. } => unreachable!("asset messages are dispatched before storage authorization"),
    }
}

#[entry_point]
pub fn query(deps: Deps, _env: Env, msg: QueryMsg) -> StdResult<Binary> {
    match msg {
        QueryMsg::Asset { message } => assets::query(deps, message),
        QueryMsg::Config {} => to_json_binary(&ConfigResponse { owner: OWNER.load(deps.storage)?.to_string() }),
        QueryMsg::Get { key } => to_json_binary(&VALUES.may_load(deps.storage, &key)?),
        QueryMsg::List { prefix, start_after, limit } => {
            let limit = limit.unwrap_or(MAX_PAGE).min(MAX_PAGE);
            if limit == 0 { return Err(StdError::generic_err("limit must be positive")); }
            // Prefix keys are contiguous in byte ordering. Start at the prefix,
            // not at the beginning of the store, and preserve query errors.
            let start = match start_after.as_deref() {
                Some(cursor) if cursor >= prefix.as_str() => Bound::exclusive(cursor),
                _ => Bound::inclusive(prefix.as_str()),
            };
            let keys = VALUES.keys(deps.storage, Some(start), None, Order::Ascending)
                .take_while(|key| key.as_ref().map(|k| k.starts_with(&prefix)).unwrap_or(true))
                .take(limit as usize)
                .collect::<StdResult<Vec<String>>>()?;
            to_json_binary(&keys)
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use cosmwasm_std::{from_json, testing::{mock_dependencies, mock_env, message_info}, coins};

    #[test]
    fn owner_only_writes_and_missing_values_are_null() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        let other = deps.api.addr_make("other");
        instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), InstantiateMsg { owner: None }).unwrap();
        let put = ExecuteMsg::Put { key: "oasis/holon/1".into(), value: "{\"name\":\"Oak\"}".into() };
        assert!(execute(deps.as_mut(), mock_env(), message_info(&other, &[]), put.clone()).is_err());
        assert!(execute(deps.as_mut(), mock_env(), message_info(&owner, &coins(1, "utest")), put.clone()).is_err());
        execute(deps.as_mut(), mock_env(), message_info(&owner, &[]), put).unwrap();
        let value: Option<String> = from_json(query(deps.as_ref(), mock_env(), QueryMsg::Get { key: "oasis/holon/1".into() }).unwrap()).unwrap();
        assert_eq!(value.as_deref(), Some("{\"name\":\"Oak\"}"));
        assert!(execute(deps.as_mut(), mock_env(), message_info(&other, &[]), ExecuteMsg::Delete { key: "oasis/holon/1".into() }).is_err());
        execute(deps.as_mut(), mock_env(), message_info(&owner, &[]), ExecuteMsg::Delete { key: "oasis/holon/1".into() }).unwrap();
        let missing: Option<String> = from_json(query(deps.as_ref(), mock_env(), QueryMsg::Get { key: "oasis/holon/1".into() }).unwrap()).unwrap();
        assert_eq!(missing, None);
    }

    #[test]
    fn prefix_pages_are_complete_and_exclusive() {
        let mut deps = mock_dependencies();
        let owner = deps.api.addr_make("owner");
        instantiate(deps.as_mut(), mock_env(), message_info(&owner, &[]), InstantiateMsg { owner: None }).unwrap();
        for i in 0..205 {
            VALUES.save(deps.as_mut().storage, &format!("oasis/{i:03}"), &"value".into()).unwrap();
        }
        VALUES.save(deps.as_mut().storage, "before", &"value".into()).unwrap();
        VALUES.save(deps.as_mut().storage, "outside", &"value".into()).unwrap();
        let mut all = Vec::<String>::new();
        loop {
            let page: Vec<String> = from_json(query(deps.as_ref(), mock_env(), QueryMsg::List {
                prefix: "oasis/".into(), start_after: all.last().cloned(), limit: Some(1000),
            }).unwrap()).unwrap();
            assert!(page.len() <= MAX_PAGE as usize);
            if page.is_empty() { break; }
            all.extend(page);
        }
        assert_eq!(all.len(), 205);
        assert_eq!(all.first().unwrap(), "oasis/000");
        assert_eq!(all.last().unwrap(), "oasis/204");
        assert!(query(deps.as_ref(), mock_env(), QueryMsg::List { prefix: "".into(), start_after: None, limit: Some(0) }).is_err());
    }
}
