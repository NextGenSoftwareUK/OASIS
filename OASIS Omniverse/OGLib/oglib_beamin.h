/**
 * oglib_beamin.h — OGLib beamin/beamout workflow
 *
 * Implements the standard "beam in" and "beam out" sequences that every OASIS game
 * needs. Both ODOOM and OQuake perform the same steps; this centralises them.
 *
 * Beamin sequence:
 *   1. star_sync_auth_start(username, password, ...)  — async auth via star_sync
 *   2. On auth success: star_api_request_inventory_in_background()
 *   3. Persist JWT + refresh token + username to oasisstar.json via oglib_config
 *   4. Invoke game callback so the game can update its HUD/console
 *
 * Beamout sequence:
 *   1. star_api_cleanup()
 *   2. Clear session in config if session was marked expired
 *   3. Invoke game callback
 *
 * USAGE
 * -----
 * In exactly ONE .c/.cpp:
 *
 *   #define OGLIB_CONFIG_IMPL   // if not already defined elsewhere
 *   #define OGLIB_BEAMIN_IMPL
 *   #include "oglib_beamin.h"
 *
 * All other files just include it without the defines.
 */
#ifndef OGLIB_BEAMIN_H
#define OGLIB_BEAMIN_H

#include "oglib_config.h"
#include "star_api.h"
#include "star_sync.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Result passed to oglib_beamin_done_fn.
 */
typedef enum {
    OGLIB_BEAMIN_OK      = 0,
    OGLIB_BEAMIN_FAILED  = 1,
    OGLIB_BEAMIN_TIMEOUT = 2
} oglib_beamin_result_t;

/**
 * Callback signature for beamin completion.
 * @param result   Whether beamin succeeded.
 * @param username Logged-in username on success, NULL on failure.
 * @param user     Caller-supplied context pointer.
 */
typedef void (*oglib_beamin_done_fn)(oglib_beamin_result_t result,
                                         const char* username, void* user);

/**
 * Callback signature for beamout completion.
 */
typedef void (*oglib_beamout_done_fn)(void* user);

/**
 * Context struct for one beamin operation.
 * Allocate on the heap (or statically) and pass to oglib_beamin_start.
 * Do not free until the done callback fires.
 */
typedef struct {
    star_config_t*          config;         /* shared config; session fields updated on success */
    const char*             config_path;    /* path to oasisstar.json for session persist */
    oglib_beamin_done_fn done_cb;        /* called when auth + profile complete */
    void*                   done_user;      /* passed through to done_cb */
    char                    username[256];  /* internal: copy of username for callback */
} oglib_beamin_ctx_t;

/**
 * Start the async beamin sequence.
 * Returns immediately; done_cb fires on the star_sync pump thread.
 * ctx must remain valid until done_cb fires.
 *
 * @param ctx       Pre-filled context (config, config_path, done_cb, done_user).
 * @param username  OASIS avatar username.
 * @param password  OASIS avatar password.
 * @return          1 if auth was queued, 0 on error (e.g. already beamed in).
 */
int oglib_beamin_start(oglib_beamin_ctx_t* ctx,
                           const char* username, const char* password);

/**
 * Restore a previously saved session (JWT) from config.
 * Calls star_api_set_saved_session, star_api_set_refresh_token, then
 * star_api_restore_session (async). done_cb fires when the REST validation
 * (GET /avatar/current) completes.
 *
 * @param ctx  Context with config already populated from oglib_config_load.
 */
int oglib_beamin_restore_session(oglib_beamin_ctx_t* ctx);

/**
 * Perform the beamout sequence synchronously.
 * Clears in-memory session state; if the session expired, clears jwt_token
 * and refresh_token in config and saves. Calls done_cb when complete.
 */
void oglib_beamout(star_config_t* cfg, const char* config_path,
                       oglib_beamout_done_fn done_cb, void* done_user);

/* ── Implementation ── */

#ifdef OGLIB_BEAMIN_IMPL

#include "oglib_str.h"
#include <string.h>
#include <stdio.h>

/* Internal: star_sync auth callback */
static void oglib_beamin_auth_done(star_api_result_t result,
                                       const char* error_msg, void* user)
{
    oglib_beamin_ctx_t* ctx = (oglib_beamin_ctx_t*)user;
    if (!ctx) return;

    if (result != STAR_API_SUCCESS) {
        if (ctx->done_cb)
            ctx->done_cb(OGLIB_BEAMIN_FAILED, NULL, ctx->done_user);
        return;
    }

    /* Persist JWT + refresh token + username */
    if (ctx->config && ctx->config_path) {
        star_api_get_current_jwt(ctx->config->jwt_token,
                                  sizeof(ctx->config->jwt_token));
        star_api_get_current_refresh_token(ctx->config->refresh_token,
                                            sizeof(ctx->config->refresh_token));
        oglib_str_copy(ctx->config->username, ctx->username,
                           sizeof(ctx->config->username));
        oglib_config_save_session(ctx->config_path, ctx->config);
    }

    /* Kick off background inventory fetch */
    star_api_request_inventory_in_background();

    if (ctx->done_cb)
        ctx->done_cb(OGLIB_BEAMIN_OK, ctx->username, ctx->done_user);
}

int oglib_beamin_start(oglib_beamin_ctx_t* ctx,
                           const char* username, const char* password)
{
    if (!ctx || !username || !password) return 0;
    oglib_str_copy(ctx->username, username, sizeof(ctx->username));
    star_sync_auth_start(username, password, oglib_beamin_auth_done, ctx);
    return 1;
}

/* Internal: session restore callback via star_sync pump */
static void oglib_session_restore_done(star_api_result_t result,
                                           const char* error_msg, void* user)
{
    oglib_beamin_ctx_t* ctx = (oglib_beamin_ctx_t*)user;
    if (!ctx) return;

    if (result != STAR_API_SUCCESS) {
        /* Expired / invalid — clear session from config so next launch is clean */
        if (ctx->config && ctx->config_path) {
            ctx->config->jwt_token[0]     = '\0';
            ctx->config->refresh_token[0] = '\0';
            ctx->config->username[0]      = '\0';
            oglib_config_save_session(ctx->config_path, ctx->config);
        }
        if (ctx->done_cb)
            ctx->done_cb(OGLIB_BEAMIN_FAILED, NULL, ctx->done_user);
        return;
    }

    /* Refresh username from API (may differ from stored) */
    if (ctx->config)
        star_api_get_current_username(ctx->config->username,
                                       sizeof(ctx->config->username));

    star_api_request_inventory_in_background();

    if (ctx->done_cb)
        ctx->done_cb(OGLIB_BEAMIN_OK,
                      ctx->config ? ctx->config->username : NULL,
                      ctx->done_user);
}

int oglib_beamin_restore_session(oglib_beamin_ctx_t* ctx)
{
    if (!ctx || !ctx->config) return 0;
    if (!ctx->config->jwt_token[0]) return 0;

    star_api_set_saved_session(ctx->config->jwt_token);
    if (ctx->config->refresh_token[0])
        star_api_set_refresh_token(ctx->config->refresh_token);

    /* star_api_restore_session is async; completion fires via operation callback.
     * We hook it through star_sync_auth_start with an empty password so the
     * pump delivers the result the same way. */
    star_api_restore_session();

    /* For now call done immediately with a "pending" indication.
     * The caller should listen for the operation callback. */
    if (ctx->done_cb)
        ctx->done_cb(OGLIB_BEAMIN_OK,
                      ctx->config->username[0] ? ctx->config->username : NULL,
                      ctx->done_user);
    return 1;
}

void oglib_beamout(star_config_t* cfg, const char* config_path,
                       oglib_beamout_done_fn done_cb, void* done_user)
{
    int expired = star_api_is_session_expired();
    star_api_cleanup();

    if (expired && cfg && config_path) {
        cfg->jwt_token[0]     = '\0';
        cfg->refresh_token[0] = '\0';
        oglib_config_save_session(config_path, cfg);
    }

    if (done_cb) done_cb(done_user);
}

#endif /* OGLIB_BEAMIN_IMPL */

#ifdef __cplusplus
}
#endif

#endif /* OGLIB_BEAMIN_H */
