-- OASIS cross-game portal nodes for OMineCraft (Minetest mod)
-- Portal frame is obsidian (same mental model as Nether portals).
-- Right-click the portal block with an OASIS Portal Key to configure the destination.

local api = dofile(minetest.get_modpath("oasis") .. "/api.lua")

local PORTAL_FRAME = "default:obsidian"
local PORTAL_NODE  = "oasis:portal"

-- Register the portal air node (the glowing fill inside the frame)
minetest.register_node(PORTAL_NODE, {
    description = "OASIS Cross-Game Portal",
    drawtype    = "nodebox",
    tiles       = { {
        name      = "oasis_portal.png",
        animation = { type = "vertical_frames", aspect_w = 16, aspect_h = 16, length = 1.5 },
    } },
    paramtype   = "light",
    light_source = 11,
    walkable    = false,
    pointable   = true,
    diggable    = false,
    groups      = { not_in_creative_inventory = 1 },
    sounds      = default and default.node_sound_glass_defaults() or {},
    on_rightclick = function(pos, node, clicker, itemstack)
        local player_name = clicker:get_player_name()
        local meta        = minetest.get_meta(pos)
        local dest_game   = meta:get_string("dest_game")
        local dest_map    = meta:get_string("dest_map")

        if dest_game == "" then
            minetest.chat_send_player(player_name,
                "[OASIS] Portal not configured. Use an OASIS Portal Key to set destination.")
            return
        end

        if not api.is_logged_in(player_name) then
            minetest.chat_send_player(player_name,
                "[OASIS] Log in first: /oasis login <username> <password>")
            return
        end

        local ppos = clicker:get_pos()
        api.request_teleport(player_name, dest_game, dest_map, ppos.x, ppos.y, ppos.z)
        minetest.chat_send_player(player_name,
            "[OASIS] Portal → " .. dest_game .. " / " .. dest_map .. " (connecting...)")
    end,
})

-- Register OASIS Portal Key — right-click a portal node to configure destination
minetest.register_craftitem("oasis:portal_key", {
    description = "OASIS Portal Key\nRight-click a portal to set its destination.",
    inventory_image = "oasis_portal_key.png",
    on_place = function(itemstack, placer, pointed_thing)
        if pointed_thing.type ~= "node" then return end
        local pos  = pointed_thing.under
        local node = minetest.get_node(pos)
        if node.name ~= PORTAL_NODE then return end

        local player_name = placer:get_player_name()
        local meta_item   = itemstack:get_meta()
        local dest_game   = meta_item:get_string("dest_game")
        local dest_map    = meta_item:get_string("dest_map")

        if dest_game == "" then
            minetest.chat_send_player(player_name,
                "[OASIS] Portal Key has no destination. Set with /oasis setportal <game> <map>")
            return
        end

        local meta = minetest.get_meta(pos)
        meta:set_string("dest_game", dest_game)
        meta:set_string("dest_map",  dest_map)
        minetest.chat_send_player(player_name,
            "[OASIS] Portal destination set: " .. dest_game .. " / " .. dest_map)
    end,
})

-- Try to light a portal when a frame is completed
local function try_light_portal(pos)
    local frame_positions = {
        { x = pos.x,     y = pos.y + 1, z = pos.z },
        { x = pos.x,     y = pos.y + 2, z = pos.z },
        { x = pos.x,     y = pos.y - 1, z = pos.z },
        { x = pos.x + 1, y = pos.y,     z = pos.z },
        { x = pos.x - 1, y = pos.y,     z = pos.z },
    }
    local frame_ok = true
    for _, fpos in ipairs(frame_positions) do
        if minetest.get_node(fpos).name ~= PORTAL_FRAME then
            frame_ok = false
            break
        end
    end
    if frame_ok then
        minetest.set_node(pos, { name = PORTAL_NODE })
    end
end

minetest.register_on_placenode(function(pos, newnode)
    if newnode.name == PORTAL_FRAME then
        try_light_portal(pos)
    end
end)
