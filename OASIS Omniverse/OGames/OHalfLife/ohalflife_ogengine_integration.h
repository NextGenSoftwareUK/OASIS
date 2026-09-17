#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void OHalfLife_STAR_Init(const char* apiBaseUrl, const char* configPath);
void OHalfLife_STAR_Tick(void);
void OHalfLife_STAR_Cleanup(void);
void OHalfLife_STAR_OnMonsterKilled(const char* classname, const char* displayName, const char* weaponUsed);
void OHalfLife_STAR_OnItemPickup(const char* classname, const char* displayName, int count);

#ifdef __cplusplus
}
#endif
