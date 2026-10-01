-- OASIS player Lua script for OMorrowind (OpenMW Lua API 0.47+)
-- Handles HUD overlay, quest popup, and inventory popup via MyGUI widgets.
--
-- Install: package into OASIS.omwaddon alongside the OpenMW executable.
-- Hook:    listed under [Player] in mod.conf

local ui      = require('openmw.ui')
local input   = require('openmw.input')
local storage = require('openmw.storage')
local core    = require('openmw.core')

local SECTION = "OASISPlayerState"
local state   = storage.playerSection(SECTION)

-- Popup state
local inv_open   = false
local quest_open = false

-- Simple overlay layout
local status_overlay = ui.create({
    layer = 'HUD',
    template = { props = { size = { x = 300, y = 60 }, relativePosition = { x = 0, y = 0 } } },
    content = ui.content({
        {
            template = { props = { size = { x = 300, y = 20 } } },
            name = 'username_label',
        },
        {
            template = { props = { size = { x = 300, y = 20 }, relativePosition = { x = 0, y = 0.05 } } },
            name = 'xp_label',
        },
    }),
})

local toast_overlay = ui.create({
    layer = 'HUD',
    template = { props = {
        relativePosition = { x = 0.5, y = 0.15 },
        relativeSize = { x = 0, y = 0 },
    } },
    name = 'toast_label',
})

local toast_expiry = 0

local function show_toast(msg)
    if toast_overlay then
        toast_overlay.layout.props.caption = msg
        toast_overlay:update()
    end
    toast_expiry = core.getSimulationTime() + 3.0
end

local function update_status()
    local username = state:get("username") or ""
    local xp       = state:get("xp")       or 0
    if status_overlay then
        local children = status_overlay.content
        if children[1] then children[1].layout.props.caption = "[ " .. username .. " ]" end
        if children[2] then children[2].layout.props.caption = "XP: " .. tostring(xp)  end
        status_overlay:update()
    end
end

local function toggle_inventory()
    inv_open = not inv_open
    -- Request fresh inventory from C++ layer
    if core.getExtensionFunction then
        local fn = core.getExtensionFunction("oasis_get_inventory")
        if fn then fn() end
    end
end

local function toggle_quests()
    quest_open = not quest_open
    if core.getExtensionFunction then
        local fn = core.getExtensionFunction("oasis_get_quests")
        if fn then fn() end
    end
end

local function on_key_press(key)
    if key.code == input.KEY.I then toggle_inventory()  return end
    if key.code == input.KEY.Q then toggle_quests()     return end
end

local function on_update(dt)
    -- Hide toast when expired
    if toast_expiry > 0 and core.getSimulationTime() > toast_expiry then
        if toast_overlay then
            toast_overlay.layout.props.caption = ""
            toast_overlay:update()
        end
        toast_expiry = 0
    end
end

-- Event from C++ integration layer: OASIS state changed
local function on_oasis_event(data)
    if data.type == "auth_ok" then
        state:set("username", data.username or "")
        state:set("xp", data.xp or 0)
        update_status()
        show_toast("OASIS: Welcome to OMorrowind, " .. (data.username or "traveller") .. ".")
    elseif data.type == "xp_update" then
        state:set("xp", data.xp or 0)
        update_status()
    elseif data.type == "toast" then
        show_toast(data.message or "")
    elseif data.type == "portal_pending" then
        show_toast("OASIS: Portal → " .. (data.game or "?") .. " / " .. (data.map or "?"))
    end
end

return {
    engineHandlers = {
        onKeyPress = on_key_press,
        onUpdate   = on_update,
    },
    eventHandlers = {
        OASISEvent = on_oasis_event,
    },
}
