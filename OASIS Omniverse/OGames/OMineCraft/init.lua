-- OMineCraft — OASIS STAR API Minetest mod
-- Main entry point. Registers chat commands, hooks player events, wires HUD.
--
-- Place this mod folder as:   minetest/mods/oasis/
-- Add to minetest.conf:       oasis_star_url = https://star-api.oasisplatform.world/api
--                              secure.http_mods = oasis

local MOD_PATH = minetest.get_modpath("oasis")
local api      = dofile(MOD_PATH .. "/api.lua")
local hud      = dofile(MOD_PATH .. "/hud.lua")
local portals  = dofile(MOD_PATH .. "/portals.lua")   -- registers nodes

local GAME_SOURCE = "OMINECRAFT"

-- Load oasisstar.json for XP table
local json_path = MOD_PATH .. "/oasisstar.json"
local json_file = io.open(json_path, "r")
local config    = json_file and minetest.parse_json(json_file:read("*a")) or {}
if json_file then json_file:close() end

local monster_xp = {}
for _, m in ipairs((config.ominecraft or {}).monsters or {}) do
    monster_xp[m.engine_id] = m.xp or 10
end

local key_items = {}
for _, k in ipairs((config.ominecraft or {}).key_items or {}) do
    key_items[k.engine_id] = k.cross_game
end

-- ── Player join / leave ──────────────────────────────────────────────────────

minetest.register_on_joinplayer(function(player)
    hud.init(player)
end)

minetest.register_on_leaveplayer(function(player)
    hud.remove(player)
    api.logout(player:get_player_name())
end)

-- ── Chat commands ─────────────────────────────────────────────────────────────

minetest.register_chatcommand("oasis", {
    params      = "<login|logout|inv|quests|setportal> [args...]",
    description = "OASIS STAR API commands",
    func = function(player_name, param)
        local args   = param:split(" ")
        local subcmd = args[1] or ""
        local player = minetest.get_player_by_name(player_name)
        if not player then return false, "Player not found." end

        if subcmd == "login" then
            local username = args[2] or ""
            local password = args[3] or ""
            if username == "" or password == "" then
                return false, "Usage: /oasis login <username> <password>"
            end
            minetest.chat_send_player(player_name, "[OASIS] Connecting...")
            api.login(player_name, username, password, function(ok, avatar_id_or_err)
                if ok then
                    hud.set_username(player, username)
                    hud.toast(player, "OASIS: Welcome to OMineCraft, " .. username .. "!")
                    minetest.chat_send_player(player_name, "[OASIS] Logged in. Avatar: " .. avatar_id_or_err)
                else
                    minetest.chat_send_player(player_name, "[OASIS] Login failed: " .. avatar_id_or_err)
                end
            end)
            return true, ""

        elseif subcmd == "logout" then
            api.logout(player_name)
            hud.set_username(player, "")
            hud.set_xp(player, 0)
            return true, "[OASIS] Logged out."

        elseif subcmd == "inv" then
            api.get_inventory(player_name, function(items)
                if #items == 0 then
                    minetest.chat_send_player(player_name, "[OASIS] Inventory empty.")
                    return
                end
                minetest.chat_send_player(player_name, "[OASIS] Cross-game inventory:")
                for _, item in ipairs(items) do
                    minetest.chat_send_player(player_name,
                        "  - " .. (item.name or item.id or "?") .. " x" .. (item.quantity or 1))
                end
            end)
            return true, ""

        elseif subcmd == "setportal" then
            local dest_game = args[2] or ""
            local dest_map  = args[3] or ""
            if dest_game == "" then
                return false, "Usage: /oasis setportal <game> <map>"
            end
            -- Store on the next portal key in hotbar — for simplicity, send chat instructions
            return true, "[OASIS] Place an OASIS Portal Key (oasis:portal_key) in your hand " ..
                         "with dest_game=" .. dest_game .. " dest_map=" .. dest_map ..
                         " — use /oasis configkey <game> <map> to stamp the key."

        elseif subcmd == "configkey" then
            local dest_game = args[2] or ""
            local dest_map  = args[3] or ""
            local inv = player:get_inventory()
            local stack = inv:get_stack("main", player:get_wield_index())
            if stack:get_name() == "oasis:portal_key" then
                local meta = stack:get_meta()
                meta:set_string("dest_game", dest_game)
                meta:set_string("dest_map",  dest_map)
                inv:set_stack("main", player:get_wield_index(), stack)
                return true, "[OASIS] Portal key configured: " .. dest_game .. " / " .. dest_map
            end
            return false, "[OASIS] Hold an OASIS Portal Key to configure it."

        else
            return false, "Subcommands: login, logout, inv, setportal, configkey"
        end
    end,
})

-- ── Item pickup hook ─────────────────────────────────────────────────────────

minetest.register_on_item_eat(function(hp_change, replace_with, itemstack, user, pointed_thing)
    if not user or not user:is_player() then return end
    local name = itemstack:get_name()
    local cross = key_items[name]
    if cross then
        api.add_item(user:get_player_name(), cross, "Key", 1)
        hud.toast(user, "[OASIS] Key item '" .. itemstack:get_description() .. "' added to cross-game inventory!")
    end
end)

-- ── Mob kill hook ─────────────────────────────────────────────────────────────

minetest.register_on_dieplayer(function(player)
    -- Not relevant for XP — handled via mobs on_die below
end)

-- Hook into mobs_redo or mobs API if available
if minetest.global_exists("mobs") and mobs.register_mob then
    local orig_on_die = mobs.on_die
    mobs.on_die = function(self, killer)
        if killer and killer:is_player() then
            local player_name = killer:get_player_name()
            local xp = monster_xp[self.name] or 10
            api.add_xp(player_name, xp, "Killed " .. (self.name or "mob"))
            local player = minetest.get_player_by_name(player_name)
            if player and xp >= 50 then
                hud.toast(player, "[OASIS] +" .. xp .. " XP  (killed " .. (self.name or "mob") .. ")")
            end
        end
        if orig_on_die then orig_on_die(self, killer) end
    end
end

minetest.log("action", "[OASIS] OMineCraft STAR API mod loaded — game source: " .. GAME_SOURCE)
