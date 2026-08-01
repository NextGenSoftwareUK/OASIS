/**
 * Optional stub for ogengine_queue_quest_level_time when NOT using the full OASIS deploy.
 * Normally BUILD QUAKE runs build-and-deploy first, which regenerates ogengine.def from C#
 * so ogengine.lib has all symbols (see Scripts/generate_ogengine_def.ps1). You only need
 * this file if you build vkQuake without running that deploy (e.g. lib has no symbol).
 * Define OQUAKE_QUEST_LEVEL_TIME_STUB when compiling this file; no-op implementation.
 */
#ifdef OQUAKE_QUEST_LEVEL_TIME_STUB

#ifdef __cplusplus
extern "C" {
#endif

void ogengine_queue_quest_level_time(const char* game_source, int level_elapsed_seconds)
{
    (void)game_source;
    (void)level_elapsed_seconds;
}

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE_QUEST_LEVEL_TIME_STUB */
