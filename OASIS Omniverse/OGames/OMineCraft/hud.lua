-- OASIS HUD overlay for OMineCraft (Minetest mod)
-- Shows username, XP, and toast notifications using Minetest's native HUD API.

local M = {}

-- Per-player HUD element IDs
local hud_ids = {}  -- player_name → { username_id, xp_id, toast_id }

local function get_ids(player)
    return hud_ids[player:get_player_name()]
end

function M.init(player)
    local name = player:get_player_name()
    local username_id = player:hud_add({
        hud_elem_type = "text",
        position      = { x = 0.01, y = 0.02 },
        offset        = { x = 0,    y = 0    },
        text          = "",
        number        = 0x00e5ff,  -- cyan
        scale         = { x = 100, y = 100 },
        alignment     = { x = 1,   y = 1   },
    })
    local xp_id = player:hud_add({
        hud_elem_type = "text",
        position      = { x = 0.99, y = 0.02 },
        offset        = { x = 0,    y = 0    },
        text          = "",
        number        = 0xffd700,  -- gold
        scale         = { x = 100, y = 100 },
        alignment     = { x = -1,  y = 1   },
    })
    local toast_id = player:hud_add({
        hud_elem_type = "text",
        position      = { x = 0.5,  y = 0.15 },
        offset        = { x = 0,    y = 0    },
        text          = "",
        number        = 0xffffff,
        scale         = { x = 200, y = 100 },
        alignment     = { x = 0,   y = 0   },
    })
    hud_ids[name] = { username_id = username_id, xp_id = xp_id, toast_id = toast_id }
end

function M.remove(player)
    local name = player:get_player_name()
    local ids  = hud_ids[name]
    if not ids then return end
    player:hud_remove(ids.username_id)
    player:hud_remove(ids.xp_id)
    player:hud_remove(ids.toast_id)
    hud_ids[name] = nil
end

function M.set_username(player, username)
    local ids = get_ids(player)
    if not ids or username == "" then return end
    player:hud_change(ids.username_id, "text", "[ " .. username .. " ]")
end

function M.set_xp(player, xp)
    local ids = get_ids(player)
    if not ids then return end
    player:hud_change(ids.xp_id, "text", "XP: " .. tostring(xp))
end

-- Toast with auto-clear after `duration` seconds (default 3)
local toast_timers = {}
function M.toast(player, message, duration)
    duration = duration or 3.0
    local name = player:get_player_name()
    local ids  = get_ids(player)
    if not ids then return end
    player:hud_change(ids.toast_id, "text", message)
    toast_timers[name] = minetest.after(duration, function()
        local p = minetest.get_player_by_name(name)
        if p and hud_ids[name] then
            p:hud_change(hud_ids[name].toast_id, "text", "")
        end
        toast_timers[name] = nil
    end)
end

return M
