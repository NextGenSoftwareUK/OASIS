//
// OHalfLife OASIS STAR API integration — hooks into HLSDK game DLL
//
// Wire-up sites:
//   GameDLLInit()                  -> OHalfLife_STAR_Init(...)
//   GameDLLShutdown()              -> OHalfLife_STAR_Cleanup()
//   StartFrame()                   -> OHalfLife_STAR_Tick()
//   CBaseMonster::Killed(...)      -> OHalfLife_STAR_OnMonsterKilled(...)
//   CBasePlayer::AddPlayerItem(.)  -> OHalfLife_STAR_OnItemPickup(...)
//
#include "ohalflife_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#include <string>
#include <mutex>
#include <atomic>

static std::atomic<bool> g_initialized{false};
static std::mutex        g_mutex;

static OGEngineConfig    g_config;
static OGEngineSyncCtx*  g_sync = nullptr;

// XP table is loaded from oasisstar.json at init — see ogengine.h
static void LoadXPTable()
{
    OGEngine_LoadConfig("oasisstar.json", &g_config);
}

extern "C" void OHalfLife_STAR_Init(const char* apiBaseUrl, const char* configPath)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    if (g_initialized.load()) return;

    g_config.apiBaseUrl  = apiBaseUrl  ? apiBaseUrl  : "https://star-api.oasisplatform.world/api";
    g_config.configPath  = configPath  ? configPath  : "oasisstar.json";

    LoadXPTable();

    OGEngineResult r = OGEngine_Init(&g_config);
    if (r != OGENGINE_OK) return;

    g_sync = OGEngine_CreateSyncCtx();
    g_initialized.store(true);
}

extern "C" void OHalfLife_STAR_Tick(void)
{
    if (!g_initialized.load()) return;
    OGEngine_Tick(g_sync);
}

extern "C" void OHalfLife_STAR_Cleanup(void)
{
    std::lock_guard<std::mutex> lock(g_mutex);
    if (!g_initialized.load()) return;

    OGEngine_DestroySyncCtx(g_sync);
    g_sync = nullptr;
    OGEngine_Shutdown();
    g_initialized.store(false);
}

extern "C" void OHalfLife_STAR_OnMonsterKilled(const char* classname,
                                                const char* displayName,
                                                const char* weaponUsed)
{
    if (!g_initialized.load()) return;

    int xp = OGEngine_LookupMonsterXP(&g_config, classname);
    if (xp > 0)
        OGEngine_AddXP(g_sync, classname, displayName, xp, weaponUsed);
}

extern "C" void OHalfLife_STAR_OnItemPickup(const char* classname,
                                             const char* displayName,
                                             int count)
{
    if (!g_initialized.load()) return;

    if (OGEngine_IsKeyItem(&g_config, classname))
        OGEngine_AddItem(g_sync, classname, displayName, count);
}
