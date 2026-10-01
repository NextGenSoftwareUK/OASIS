-- OASIS API bridge for OMineCraft (Minetest mod)
-- Uses minetest.request_http_api() to call the OASIS STAR API directly from Lua.
-- Minetest's HTTP API is the cleanest integration of any OGame — no C layer needed.

local M = {}

local http         = minetest.request_http_api()
local STAR_URL     = minetest.settings:get("oasis_star_url") or "https://star-api.oasisplatform.world/api"
local GAME_SOURCE  = "OMINECRAFT"
local AUTH_TIMEOUT = 10
local tokens       = {}   -- player_name → { jwt, avatar_id }

local function json(t) return minetest.write_json(t) end
local function parse(s)
    local ok, t = pcall(minetest.parse_json, s)
    return ok and t or nil
end

-- POST helper
local function post(endpoint, body, callback)
    if not http then
        minetest.log("warning", "[OASIS] HTTP API unavailable — add 'oasis' to secure.http_mods in minetest.conf")
        return
    end
    http.fetch({
        url     = STAR_URL .. endpoint,
        method  = "POST",
        data    = json(body),
        timeout = AUTH_TIMEOUT,
        extra_headers = { "Content-Type: application/json" },
    }, function(res)
        local data = parse(res.data) or {}
        callback(res.code == 200, data)
    end)
end

-- GET helper
local function get(endpoint, jwt, callback)
    if not http then return end
    http.fetch({
        url     = STAR_URL .. endpoint,
        method  = "GET",
        timeout = AUTH_TIMEOUT,
        extra_headers = { "Authorization: Bearer " .. (jwt or "") },
    }, function(res)
        local data = parse(res.data) or {}
        callback(res.code == 200, data)
    end)
end

-- Log in a player to OASIS
function M.login(player_name, username, password, callback)
    post("/Avatar/Login", { username = username, password = password }, function(ok, data)
        if ok and data.token then
            tokens[player_name] = { jwt = data.token, avatar_id = data.avatarId or "" }
            callback(true, data.avatarId or "")
        else
            callback(false, (data.message or "login failed"))
        end
    end)
end

-- Add XP for a player
function M.add_xp(player_name, amount, reason)
    local t = tokens[player_name]
    if not t then return end
    post("/STAR/AddXP", { avatarId = t.avatar_id, amount = amount, reason = reason, gameSource = GAME_SOURCE }, function() end)
end

-- Add item to cross-game inventory
function M.add_item(player_name, item_id, item_type, quantity)
    local t = tokens[player_name]
    if not t then return end
    post("/STAR/AddItem", {
        avatarId  = t.avatar_id,
        itemId    = item_id,
        itemType  = item_type,
        quantity  = quantity or 1,
        gameSource = GAME_SOURCE,
    }, function() end)
end

-- Fetch cross-game inventory
function M.get_inventory(player_name, callback)
    local t = tokens[player_name]
    if not t then callback({}) return end
    get("/STAR/GetInventory?avatarId=" .. t.avatar_id, t.jwt, function(ok, data)
        callback(ok and (data.items or {}) or {})
    end)
end

-- Request a cross-game teleport (portal)
function M.request_teleport(player_name, dest_game, dest_map, x, y, z)
    local t = tokens[player_name]
    if not t then return end
    post("/STAR/RegisterPortalRequest", {
        avatarId  = t.avatar_id,
        srcGame   = GAME_SOURCE,
        destGame  = dest_game,
        destMap   = dest_map,
        destX     = x or 0,
        destY     = y or 0,
        destZ     = z or 0,
    }, function() end)
end

function M.is_logged_in(player_name)
    return tokens[player_name] ~= nil
end

function M.get_token(player_name)
    local t = tokens[player_name]
    return t and t.jwt or nil
end

function M.logout(player_name)
    tokens[player_name] = nil
end

return M
