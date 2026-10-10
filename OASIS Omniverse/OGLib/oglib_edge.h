/** Shared native game binding. OGEngineClient owns identity, persistence and synchronization. */
#ifndef OGLIB_EDGE_H
#define OGLIB_EDGE_H

#include "ogengine.h"
#include "oglib_json.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

typedef struct {
    int enabled; /* -1 reads OASIS DNA, then defaults on in the Edge release. */
    char device_id[64];
    char database_path[1024];
    char public_key[2048];
    char scopes[512];
    int lifetime_minutes;
} oglib_edge_settings_t;

#define OGLIB_EDGE_SETTINGS_DEFAULT { -1, "", "", "", "avatar,holon,inventory,quest,karma,nft,geonft,geohotspot", 1440 }

static inline void oglib_edge_load_json(oglib_edge_settings_t* settings, const char* json)
{
    char value[32];
    if (oglib_json_extract(json, "offline_sync_enabled", value, sizeof(value))) {
        if (strcmp(value, "true") == 0 || strcmp(value, "1") == 0) settings->enabled = 1;
        else if (strcmp(value, "false") == 0 || strcmp(value, "0") == 0) settings->enabled = 0;
        else if (strcmp(value, "-1") == 0) settings->enabled = -1;
        else settings->enabled = -2; /* Rejected by the ABI, never silently disabled. */
    }
    oglib_json_extract(json, "edge_device_id", settings->device_id, sizeof(settings->device_id));
    oglib_json_extract(json, "edge_database_path", settings->database_path, sizeof(settings->database_path));
    oglib_json_extract(json, "offline_grant_public_key", settings->public_key, sizeof(settings->public_key));
    if (strstr(json, "\"offline_scopes\""))
        oglib_json_extract(json, "offline_scopes", settings->scopes, sizeof(settings->scopes));
    if (oglib_json_extract(json, "offline_grant_lifetime_minutes", value, sizeof(value)))
        settings->lifetime_minutes = atoi(value);
}

static inline void oglib_edge_write_string(FILE* file, const char* key, const char* value)
{
    const unsigned char* p = (const unsigned char*)value;
    fprintf(file, "  \"%s\": \"", key);
    for (; *p; ++p) {
        if (*p == '"' || *p == '\\') fprintf(file, "\\%c", *p);
        else if (*p == '\n') fputs("\\n", file);
        else if (*p == '\r') fputs("\\r", file);
        else if (*p == '\t') fputs("\\t", file);
        else fputc(*p, file);
    }
    fputs("\",\n", file);
}

/* Append fields inside the game's existing config object; every field has a trailing comma. */
static inline void oglib_edge_save_json(FILE* file, const oglib_edge_settings_t* settings)
{
    fprintf(file, "  \"offline_sync_enabled\": %d,\n", settings->enabled);
    oglib_edge_write_string(file, "edge_device_id", settings->device_id);
    oglib_edge_write_string(file, "edge_database_path", settings->database_path);
    oglib_edge_write_string(file, "offline_grant_public_key", settings->public_key);
    oglib_edge_write_string(file, "offline_scopes", settings->scopes);
    fprintf(file, "  \"offline_grant_lifetime_minutes\": %d,\n", settings->lifetime_minutes);
}

static inline ogengine_result_t oglib_edge_configure(const oglib_edge_settings_t* settings)
{
    ogengine_edge_config_t config;
    memset(&config, 0, sizeof(config));
    config.struct_size = sizeof(config);
    config.version = OGENGINE_EDGE_CONFIG_VERSION;
    config.offline_sync_enabled = settings->enabled;
    config.device_id = settings->device_id;
    config.database_path = settings->database_path;
    config.offline_grant_public_key = settings->public_key;
    config.offline_scopes_csv = settings->scopes;
    config.offline_grant_lifetime_minutes = settings->lifetime_minutes;
    return ogengine_configure_edge(&config);
}

/* UI command adapter only. Runtime changes, drain checks and errors are owned by OGEngineClient. */
static inline void oglib_edge_status_text(const ogengine_edge_status_t* status, int capabilities, char* message, size_t size)
{
    const char* state = !(capabilities & 1) || !(capabilities & 2) ? "Remote only" :
        !status->initialized ? "Awaiting beam-in" : status->connectivity == 0 ? "Working offline" :
        status->connectivity == 1 ? "Connecting" : status->synchronization == 4 ? "Sync needs attention" :
        status->synchronization == 1 || status->pending_operation_count > 0 ? "Synchronizing" : "Online";
    if (status->pending_operation_count > 0)
        snprintf(message, size, "%s (%lld pending)", state, (long long)status->pending_operation_count);
    else snprintf(message, size, "%s", state);
}

static inline void oglib_edge_command(const char* command, char* message, size_t size)
{
    ogengine_edge_status_t status;
    int capabilities = ogengine_get_edge_capabilities();
    if (!command || strcmp(command, "status") == 0) {
        ogengine_get_edge_status(&status);
        snprintf(message, size, "Offline sync: %s. Runtime: %s. Pending actions: %lld.",
            !(capabilities & 1) ? "unavailable (Remote-Only release)" : (capabilities & 2) ? "enabled" : "disabled",
            !status.initialized ? "awaiting beam-in" : status.connectivity == 0 ? "working offline" :
                status.synchronization == 1 ? "synchronizing" : "online",
            (long long)status.pending_operation_count);
    } else if (strcmp(command, "cancel") == 0) {
        snprintf(message, size, "No offline-sync setting was changed.");
    } else if (strcmp(command, "on") == 0 || strcmp(command, "off") == 0 || strcmp(command, "sync-and-off") == 0) {
        ogengine_result_t result = ogengine_request_offline_sync(strcmp(command, "on") == 0,
            strcmp(command, "sync-and-off") == 0);
        snprintf(message, size, "%s", result == OGENGINE_SUCCESS ? "Applying offline-sync setting..." : ogengine_get_last_error());
    } else {
        snprintf(message, size, "star offline status | on | off | sync-and-off | cancel");
    }
}

static inline int oglib_edge_finish_change(oglib_edge_settings_t* settings, char* message, size_t size)
{
    int enabled = 0;
    int result = ogengine_poll_offline_sync_change(&enabled);
    if (result == 1) {
        settings->enabled = enabled;
        snprintf(message, size, "Offline sync %s.", enabled ? "enabled" : "disabled (remote-only)");
    } else if (result < 0) {
        snprintf(message, size, "%s", ogengine_get_last_error());
    }
    return result;
}

#endif
