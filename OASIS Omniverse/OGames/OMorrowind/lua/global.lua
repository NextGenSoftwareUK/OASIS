-- OASIS global Lua script for OMorrowind (OpenMW Lua API 0.47+)
-- Handles cross-game portal cell transitions via the engine extension.
--
-- Install: package into OASIS.omwaddon alongside the OpenMW executable.
-- Hook:    listed under [Global] in mod.conf

local core    = require('openmw.core')
local world   = require('openmw.world')
local storage = require('openmw.storage')

local OASIS_VERSION = "1.0.0"
local PORTAL_SCRIPT = "oasis_portal_enter"
local SECTION       = "OASISGlobalState"

-- Persistent storage: last portal destination per player
local state = storage.globalSection(SECTION)

-- Called when an actor activates an object with oasis_portal_enter script
local function on_portal_activated(player, portal_object)
    local dest_game = portal_object:getStringVariable("dest_game")   or ""
    local dest_map  = portal_object:getStringVariable("dest_map")    or ""
    local dest_x    = portal_object:getFloatVariable("dest_x")       or 0.0
    local dest_y    = portal_object:getFloatVariable("dest_y")       or 0.0
    local dest_z    = portal_object:getFloatVariable("dest_z")       or 0.0

    if dest_game == "" then return end

    -- Persist destination so the player.lua HUD can show it
    state:set("pending_portal_game", dest_game)
    state:set("pending_portal_map",  dest_map)

    -- Fire OASIS teleport request via C++ integration (custom engine function)
    -- Signature: oasis_request_teleport(game, map, x, y, z)
    if core.getExtensionFunction then
        local fn = core.getExtensionFunction("oasis_request_teleport")
        if fn then
            fn(dest_game, dest_map, dest_x, dest_y, dest_z)
        end
    end
end

-- Object activation handler — check if it's an OASIS portal
local function on_activate(object, actor)
    if not actor or actor.type ~= core.ActorType.Player then return end
    local script_name = object:getScript()
    if script_name and script_name:lower():find(PORTAL_SCRIPT, 1, true) then
        on_portal_activated(actor, object)
    end
end

-- Cell change handler — log for OASIS quest system
local function on_cell_changed(player, cell)
    local cell_name = cell and cell.name or "unknown"
    if core.getExtensionFunction then
        local fn = core.getExtensionFunction("oasis_on_cell_enter")
        if fn then fn(cell_name) end
    end
end

return {
    engineHandlers = {
        onActivate    = on_activate,
        onCellChange  = on_cell_changed,
    },
    eventHandlers = {},
}
