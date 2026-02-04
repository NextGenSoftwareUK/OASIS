use cosmwasm_std::{
    entry_point, Binary, Deps, DepsMut, Env, MessageInfo, Response, StdResult, to_binary,
    Addr, Uint128,
};
use cw_storage_plus::{Item, Map};
use schemars::JsonSchema;
use serde::{Deserialize, Serialize};

/// Avatar structure
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct Avatar {
    pub id: String,
    pub username: String,
    pub email: String,
    pub first_name: String,
    pub last_name: String,
    pub created_date: u64,
    pub modified_date: u64,
}

/// AvatarDetail structure
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct AvatarDetail {
    pub id: String,
    pub username: String,
    pub email: String,
    pub karma_akashic_records: String,
    pub xp: u64,
    pub level: u64,
    pub created_date: u64,
    pub modified_date: u64,
}

/// Holon structure
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct Holon {
    pub id: String,
    pub name: String,
    pub description: String,
    pub parent_id: String,
    pub holon_type: u8,
    pub created_date: u64,
    pub modified_date: u64,
}

/// Instantiate message
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
pub struct InstantiateMsg {
    pub owner: Option<String>,
}

/// Execute messages
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum ExecuteMsg {
    CreateAvatar {
        avatar_id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
    },
    SaveAvatarDetail {
        avatar_id: String,
        username: String,
        email: String,
        karma_akashic_records: String,
        xp: u64,
        level: u64,
    },
    DeleteAvatar {
        avatar_id: String,
        soft_delete: bool,
    },
    SaveHolon {
        holon_id: String,
        name: String,
        description: String,
        parent_id: String,
        holon_type: u8,
    },
    DeleteHolon {
        holon_id: String,
    },
}

/// Query messages
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq, JsonSchema)]
#[serde(rename_all = "snake_case")]
pub enum QueryMsg {
    GetAvatarById { avatar_id: String },
    GetAvatarByUsername { username: String },
    GetAvatarByEmail { email: String },
    GetAllAvatars {},
    GetAvatarDetail { avatar_id: String },
    GetAvatarDetailByUsername { username: String },
    GetAvatarDetailByEmail { email: String },
    GetAllAvatarDetails {},
    GetHolon { holon_id: String },
    GetHolonsForParent { parent_id: String },
    GetHolonsByMetadata { meta_key: String, meta_value: String },
    GetAllHolons {},
    Search { query: String },
}

// Storage
const AVATARS: Map<&str, Avatar> = Map::new("avatars");
const AVATARS_BY_USERNAME: Map<&str, String> = Map::new("avatars_by_username");
const AVATARS_BY_EMAIL: Map<&str, String> = Map::new("avatars_by_email");
const AVATAR_DETAILS: Map<&str, AvatarDetail> = Map::new("avatar_details");
const AVATAR_DETAILS_BY_USERNAME: Map<&str, String> = Map::new("avatar_details_by_username");
const AVATAR_DETAILS_BY_EMAIL: Map<&str, String> = Map::new("avatar_details_by_email");
const HOLONS: Map<&str, Holon> = Map::new("holons");
const HOLONS_BY_PARENT: Map<&str, Vec<String>> = Map::new("holons_by_parent");

#[entry_point]
pub fn instantiate(
    _deps: DepsMut,
    _env: Env,
    _info: MessageInfo,
    _msg: InstantiateMsg,
) -> StdResult<Response> {
    Ok(Response::default())
}

#[entry_point]
pub fn execute(
    deps: DepsMut,
    env: Env,
    info: MessageInfo,
    msg: ExecuteMsg,
) -> StdResult<Response> {
    match msg {
        ExecuteMsg::CreateAvatar {
            avatar_id,
            username,
            email,
            first_name,
            last_name,
        } => create_avatar(deps, env, avatar_id, username, email, first_name, last_name),
        ExecuteMsg::SaveAvatarDetail {
            avatar_id,
            username,
            email,
            karma_akashic_records,
            xp,
            level,
        } => save_avatar_detail(deps, env, avatar_id, username, email, karma_akashic_records, xp, level),
        ExecuteMsg::DeleteAvatar { avatar_id, soft_delete: _ } => delete_avatar(deps, avatar_id),
        ExecuteMsg::SaveHolon {
            holon_id,
            name,
            description,
            parent_id,
            holon_type,
        } => save_holon(deps, env, holon_id, name, description, parent_id, holon_type),
        ExecuteMsg::DeleteHolon { holon_id } => delete_holon(deps, holon_id),
    }
}

#[entry_point]
pub fn query(deps: Deps, _env: Env, msg: QueryMsg) -> StdResult<Binary> {
    match msg {
        QueryMsg::GetAvatarById { avatar_id } => {
            let avatar = AVATARS.may_load(deps.storage, &avatar_id)?;
            to_binary(&avatar)
        }
        QueryMsg::GetAvatarByUsername { username } => {
            let avatar_id = AVATARS_BY_USERNAME.may_load(deps.storage, &username)?;
            let avatar = avatar_id.and_then(|id| AVATARS.may_load(deps.storage, &id).ok()?).flatten();
            to_binary(&avatar)
        }
        QueryMsg::GetAvatarByEmail { email } => {
            let avatar_id = AVATARS_BY_EMAIL.may_load(deps.storage, &email)?;
            let avatar = avatar_id.and_then(|id| AVATARS.may_load(deps.storage, &id).ok()?).flatten();
            to_binary(&avatar)
        }
        QueryMsg::GetAllAvatars {} => {
            // In production, would maintain a list of all avatar IDs
            to_binary(&Vec::<Avatar>::new())
        }
        QueryMsg::GetAvatarDetail { avatar_id } => {
            let avatar_detail = AVATAR_DETAILS.may_load(deps.storage, &avatar_id)?;
            to_binary(&avatar_detail)
        }
        QueryMsg::GetAvatarDetailByUsername { username } => {
            let avatar_detail_id = AVATAR_DETAILS_BY_USERNAME.may_load(deps.storage, &username)?;
            let avatar_detail = avatar_detail_id.and_then(|id| AVATAR_DETAILS.may_load(deps.storage, &id).ok()?).flatten();
            to_binary(&avatar_detail)
        }
        QueryMsg::GetAvatarDetailByEmail { email } => {
            let avatar_detail_id = AVATAR_DETAILS_BY_EMAIL.may_load(deps.storage, &email)?;
            let avatar_detail = avatar_detail_id.and_then(|id| AVATAR_DETAILS.may_load(deps.storage, &id).ok()?).flatten();
            to_binary(&avatar_detail)
        }
        QueryMsg::GetAllAvatarDetails {} => {
            // In production, would maintain a list of all avatar detail IDs
            to_binary(&Vec::<AvatarDetail>::new())
        }
        QueryMsg::GetHolon { holon_id } => {
            let holon = HOLONS.may_load(deps.storage, &holon_id)?;
            to_binary(&holon)
        }
        QueryMsg::GetHolonsForParent { parent_id } => {
            let holon_ids = HOLONS_BY_PARENT.may_load(deps.storage, &parent_id)?.unwrap_or_default();
            let holons: Vec<Holon> = holon_ids
                .iter()
                .filter_map(|id| HOLONS.may_load(deps.storage, id).ok()?.flatten())
                .collect();
            to_binary(&holons)
        }
        QueryMsg::GetHolonsByMetadata { meta_key: _, meta_value: _ } => {
            // In production, would maintain metadata index
            to_binary(&Vec::<Holon>::new())
        }
        QueryMsg::GetAllHolons {} => {
            // In production, would maintain a list of all holon IDs
            to_binary(&Vec::<Holon>::new())
        }
        QueryMsg::Search { query: _ } => {
            // In production, would implement full-text search
            to_binary(&Vec::<Holon>::new())
        }
    }
}

fn create_avatar(
    deps: DepsMut,
    env: Env,
    avatar_id: String,
    username: String,
    email: String,
    first_name: String,
    last_name: String,
) -> StdResult<Response> {
    let now = env.block.time.seconds();
    let avatar = Avatar {
        id: avatar_id.clone(),
        username: username.clone(),
        email: email.clone(),
        first_name,
        last_name,
        created_date: now,
        modified_date: now,
    };

    AVATARS.save(deps.storage, &avatar_id, &avatar)?;
    AVATARS_BY_USERNAME.save(deps.storage, &username, &avatar_id)?;
    AVATARS_BY_EMAIL.save(deps.storage, &email, &avatar_id)?;

    Ok(Response::default())
}

fn save_avatar_detail(
    deps: DepsMut,
    env: Env,
    avatar_id: String,
    username: String,
    email: String,
    karma_akashic_records: String,
    xp: u64,
    level: u64,
) -> StdResult<Response> {
    let now = env.block.time.seconds();
    let avatar_detail = AvatarDetail {
        id: avatar_id.clone(),
        username: username.clone(),
        email: email.clone(),
        karma_akashic_records,
        xp,
        level,
        created_date: now,
        modified_date: now,
    };

    AVATAR_DETAILS.save(deps.storage, &avatar_id, &avatar_detail)?;
    AVATAR_DETAILS_BY_USERNAME.save(deps.storage, &username, &avatar_id)?;
    AVATAR_DETAILS_BY_EMAIL.save(deps.storage, &email, &avatar_id)?;

    Ok(Response::default())
}

fn delete_avatar(deps: DepsMut, avatar_id: String) -> StdResult<Response> {
    AVATARS.remove(deps.storage, &avatar_id);
    Ok(Response::default())
}

fn save_holon(
    deps: DepsMut,
    env: Env,
    holon_id: String,
    name: String,
    description: String,
    parent_id: String,
    holon_type: u8,
) -> StdResult<Response> {
    let now = env.block.time.seconds();
    let holon = Holon {
        id: holon_id.clone(),
        name,
        description,
        parent_id: parent_id.clone(),
        holon_type,
        created_date: now,
        modified_date: now,
    };

    HOLONS.save(deps.storage, &holon_id, &holon)?;

    // Add to parent index
    let mut parent_holons = HOLONS_BY_PARENT
        .may_load(deps.storage, &parent_id)?
        .unwrap_or_default();
    parent_holons.push(holon_id);
    HOLONS_BY_PARENT.save(deps.storage, &parent_id, &parent_holons)?;

    Ok(Response::default())
}

fn delete_holon(deps: DepsMut, holon_id: String) -> StdResult<Response> {
    HOLONS.remove(deps.storage, &holon_id);
    Ok(Response::default())
}

