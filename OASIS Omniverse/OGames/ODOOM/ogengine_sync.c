/**
 * OASIS STAR API - Generic game integration layer implementation.
 * Async auth, async inventory (with optional local-item sync), single-item sync.
 * Compiles on Windows (Win32 threads) and elsewhere (pthreads).
 */

#include "ogengine_sync.h"
#include <string.h>
#include <stdlib.h>

#ifndef OGENGINE_HAS_SEND_ITEM
extern ogengine_result_t ogengine_send_item_to_avatar(const char*, const char*, int, const char*);
extern ogengine_result_t ogengine_send_item_to_clan(const char*, const char*, int, const char*);
#endif

/* Optional stub for ogengine_queue_quest_level_time when not linking ogengine.dll (e.g. vkQuake with older lib).
 * Define OGENGINE_PROVIDE_QUEST_LEVEL_TIME_STUB in the build to resolve LNK2001; otherwise link with a
 * STAR API build that exports this (OGEngineClient.cs UnmanagedCallersOnly). */
#ifdef OGENGINE_PROVIDE_QUEST_LEVEL_TIME_STUB
void ogengine_queue_quest_level_time(const char* game_source, int level_elapsed_seconds)
{
    (void)game_source;
    (void)level_elapsed_seconds;
}
#endif

#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

/* Safe copy; always null-terminates, truncates to size-1 */
static void str_copy(char* dst, const char* src, size_t size) {
    if (!size) return;
    if (!src) { dst[0] = '\0'; return; }
    size_t n = 0;
    while (n + 1 < size && src[n]) {
        dst[n] = src[n];
        n++;
    }
    dst[n] = '\0';
}

/* ---------------------------------------------------------------------------
 * Auth state
 * --------------------------------------------------------------------------- */
#define AUTH_USERNAME_SIZE  64
#define AUTH_AVATAR_SIZE    64
#define AUTH_ERROR_SIZE     256
#define AUTH_JWT_SIZE       2048

static char g_auth_username_buf[AUTH_USERNAME_SIZE];
static char g_auth_password_buf[64];
static int  g_auth_in_progress = 0;
static int  g_auth_has_result = 0;
static int  g_auth_success = 0;
static char g_auth_username_out[AUTH_USERNAME_SIZE];
static char g_auth_avatar_id_out[AUTH_AVATAR_SIZE];
static char g_auth_jwt_out[AUTH_JWT_SIZE];
static char g_auth_error_msg[AUTH_ERROR_SIZE];
static ogengine_sync_auth_on_done_fn g_auth_on_done = NULL;
static void* g_auth_on_done_user = NULL;

#ifdef _WIN32
static CRITICAL_SECTION g_auth_lock;
static HANDLE g_auth_thread = NULL;
#else
static pthread_mutex_t g_auth_lock = PTHREAD_MUTEX_INITIALIZER;
static pthread_t g_auth_thread = 0;
#endif

#ifdef _WIN32
static DWORD WINAPI auth_thread_proc(LPVOID param) {
#else
static void* auth_thread_proc(void* param) {
#endif
    char user[AUTH_USERNAME_SIZE], pass[64];
    ogengine_result_t auth_result = OGENGINE_ERROR_NOT_INITIALIZED;
    ogengine_result_t avatar_result = OGENGINE_ERROR_NOT_INITIALIZED;
    char avatar_id[AUTH_AVATAR_SIZE] = {0};
    const char* err = NULL;

    (void)param;
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#endif
    str_copy(user, g_auth_username_buf, sizeof(user));
    str_copy(pass, g_auth_password_buf, sizeof(pass));
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif

    /* Authenticate and capture JWT in one call so games can persist to oasisstar.json (no dependency on get_current_jwt export). */
    auth_result = ogengine_authenticate_with_jwt_out(user, pass, g_auth_jwt_out, sizeof(g_auth_jwt_out));
    if (auth_result == OGENGINE_SUCCESS) {
        avatar_result = ogengine_get_avatar_id(avatar_id, sizeof(avatar_id));
        if (avatar_result != OGENGINE_SUCCESS)
            err = ogengine_get_last_error();
    } else {
        err = ogengine_get_last_error();
    }

#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    g_auth_in_progress = 0;
    g_auth_has_result = 1;
    g_auth_success = (auth_result == OGENGINE_SUCCESS && avatar_result == OGENGINE_SUCCESS) ? 1 : 0;
    str_copy(g_auth_username_out, user, sizeof(g_auth_username_out));
    str_copy(g_auth_avatar_id_out, avatar_id, sizeof(g_auth_avatar_id_out));
    str_copy(g_auth_error_msg, err ? err : "", sizeof(g_auth_error_msg));
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
    return 0;
#else
    pthread_mutex_unlock(&g_auth_lock);
    return NULL;
#endif
}

void ogengine_sync_auth_start(const char* username, const char* password, ogengine_sync_auth_on_done_fn on_done, void* user_data) {
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    if (g_auth_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_auth_lock);
#else
        pthread_mutex_unlock(&g_auth_lock);
#endif
        return;
    }
    /* Thread finished but ogengine_sync_pump() has not run the on_done callback yet — do not clear buffers or start a second SSO. */
    if (g_auth_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_auth_lock);
#else
        pthread_mutex_unlock(&g_auth_lock);
#endif
        return;
    }
    g_auth_has_result = 0;
    g_auth_on_done = on_done;
    g_auth_on_done_user = user_data;
    str_copy(g_auth_username_buf, username ? username : "", sizeof(g_auth_username_buf));
    str_copy(g_auth_password_buf, password ? password : "", sizeof(g_auth_password_buf));
    g_auth_in_progress = 1;
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
    g_auth_thread = CreateThread(NULL, 0, auth_thread_proc, NULL, 0, NULL);
#else
    pthread_mutex_unlock(&g_auth_lock);
    pthread_create(&g_auth_thread, NULL, auth_thread_proc, NULL);
#endif
}

int ogengine_sync_auth_poll(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    if (g_auth_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_auth_lock);
#else
        pthread_mutex_unlock(&g_auth_lock);
#endif
        return 0;
    }
    if (g_auth_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_auth_lock);
#else
        pthread_mutex_unlock(&g_auth_lock);
#endif
        return 1;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
    return -1;
}

int ogengine_sync_auth_get_result(int* success_out,
    char* username_buf, size_t username_size,
    char* avatar_id_buf, size_t avatar_id_size,
    char* error_msg_buf, size_t error_msg_size) {
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    if (!g_auth_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_auth_lock);
#else
        pthread_mutex_unlock(&g_auth_lock);
#endif
        return 0;
    }
    if (success_out) *success_out = g_auth_success;
    if (username_buf && username_size) str_copy(username_buf, g_auth_username_out, username_size);
    if (avatar_id_buf && avatar_id_size) str_copy(avatar_id_buf, g_auth_avatar_id_out, avatar_id_size);
    if (error_msg_buf && error_msg_size) str_copy(error_msg_buf, g_auth_error_msg, error_msg_size);
    g_auth_has_result = 0;
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
    return 1;
}

void ogengine_sync_auth_get_result_jwt(char* jwt_buf, size_t jwt_size) {
    if (!jwt_buf || !jwt_size) return;
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    str_copy(jwt_buf, g_auth_jwt_out, jwt_size);
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
}

int ogengine_sync_auth_in_progress(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    int in_progress = g_auth_in_progress;
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
    return in_progress;
}

void ogengine_sync_auth_force_reset(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    g_auth_in_progress = 0;
    g_auth_has_result = 0;
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
}

/* ---------------------------------------------------------------------------
 * Inventory state
 * --------------------------------------------------------------------------- */
#define INV_ERROR_SIZE 256

static ogengine_sync_local_item_t* g_inv_local_items = NULL;
static int g_inv_local_count = 0;
static char g_inv_default_game_source[64] = {0};
static int g_inv_in_progress = 0;
static int g_inv_has_result = 0;
static ogengine_item_list_t* g_inv_list = NULL;
static ogengine_result_t g_inv_result = OGENGINE_ERROR_NOT_INITIALIZED;
static char g_inv_error_msg[INV_ERROR_SIZE] = {0};
static char g_inv_add_item_error[INV_ERROR_SIZE] = {0}; /* first add_item failure reason (e.g. "Avatar ID is not set...") */
static ogengine_sync_inventory_on_done_fn g_inv_on_done = NULL;
static void* g_inv_on_done_user = NULL;

/* Optional add_item log callback: set by ogengine_sync_set_add_item_log_cb; invoked from main thread. */
static ogengine_sync_add_item_log_fn g_add_item_log_cb = NULL;
static void* g_add_item_log_user = NULL;
#define ADD_ITEM_LOG_NAMES_MAX 32
#define ADD_ITEM_LOG_NAME_SIZE 128
static char g_inv_add_item_log_names[ADD_ITEM_LOG_NAMES_MAX][ADD_ITEM_LOG_NAME_SIZE];
static int g_inv_add_item_log_count = 0;
static int g_inv_add_item_log_success = 0;
static char g_inv_add_item_log_error[INV_ERROR_SIZE] = {0};

#ifdef _WIN32
static CRITICAL_SECTION g_inv_lock;
static HANDLE g_inv_thread = NULL;
#else
static pthread_mutex_t g_inv_lock = PTHREAD_MUTEX_INITIALIZER;
static pthread_t g_inv_thread = 0;
#endif

static int g_sync_initialized = 0;

#ifdef _WIN32
static CRITICAL_SECTION g_send_lock;
static HANDLE g_send_thread = NULL;
#else
static pthread_mutex_t g_send_lock = PTHREAD_MUTEX_INITIALIZER;
static pthread_t g_send_thread = 0;
#endif

#define SEND_TARGET_SIZE 256
#define SEND_ITEM_NAME_SIZE 256
#define SEND_ITEM_ID_SIZE 64
#define SEND_ERROR_SIZE 384

static char g_send_target_buf[SEND_TARGET_SIZE];
static char g_send_item_name_buf[SEND_ITEM_NAME_SIZE];
static char g_send_item_id_buf[SEND_ITEM_ID_SIZE];
static int g_send_quantity = 1;
static int g_send_to_clan = 0;
static int g_send_in_progress = 0;
static int g_send_has_result = 0;
static int g_send_success = 0;
static char g_send_error_msg[SEND_ERROR_SIZE] = {0};
static ogengine_sync_send_item_on_done_fn g_send_on_done = NULL;
static void* g_send_on_done_user = NULL;

#define USE_ITEM_NAME_SIZE 256
#define USE_CONTEXT_SIZE 128
#define USE_ERROR_SIZE 384
#ifdef _WIN32
static CRITICAL_SECTION g_use_lock;
static HANDLE g_use_thread = NULL;
#else
static pthread_mutex_t g_use_lock = PTHREAD_MUTEX_INITIALIZER;
static pthread_t g_use_thread = 0;
#endif
static char g_use_item_name_buf[USE_ITEM_NAME_SIZE];
static char g_use_context_buf[USE_CONTEXT_SIZE];
static int g_use_in_progress = 0;
static int g_use_has_result = 0;
static int g_use_success = 0;
static char g_use_error_msg[USE_ERROR_SIZE] = {0};
static ogengine_sync_use_item_on_done_fn g_use_on_done = NULL;
static void* g_use_on_done_user = NULL;

#ifdef _WIN32
static DWORD WINAPI use_item_thread_proc(LPVOID param) {
#else
static void* use_item_thread_proc(void* param) {
#endif
    char item_name[USE_ITEM_NAME_SIZE], context[USE_CONTEXT_SIZE];
    int used = 0;
    const char* err = NULL;
    (void)param;
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#endif
#ifndef _WIN32
    pthread_mutex_lock(&g_use_lock);
#endif
    str_copy(item_name, g_use_item_name_buf, sizeof(item_name));
    str_copy(context, g_use_context_buf, sizeof(context));
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
#else
    pthread_mutex_unlock(&g_use_lock);
#endif
    ogengine_queue_use_item(item_name, context[0] ? context : "unknown");
    used = (ogengine_flush_use_item_jobs() == OGENGINE_SUCCESS);
    if (!used)
        err = ogengine_get_last_error();
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#else
    pthread_mutex_lock(&g_use_lock);
#endif
    g_use_in_progress = 0;
    g_use_has_result = 1;
    g_use_success = used ? 1 : 0;
    str_copy(g_use_error_msg, err ? err : "", sizeof(g_use_error_msg));
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
    return 0;
#else
    pthread_mutex_unlock(&g_use_lock);
    return NULL;
#endif
}

void ogengine_sync_use_item_start(const char* item_name, const char* context, ogengine_sync_use_item_on_done_fn on_done, void* user_data) {
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#else
    pthread_mutex_lock(&g_use_lock);
#endif
    if (g_use_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_use_lock);
#else
        pthread_mutex_unlock(&g_use_lock);
#endif
        return;
    }
    g_use_has_result = 0;
    g_use_on_done = on_done;
    g_use_on_done_user = user_data;
    str_copy(g_use_item_name_buf, item_name ? item_name : "", sizeof(g_use_item_name_buf));
    str_copy(g_use_context_buf, context ? context : "", sizeof(g_use_context_buf));
    g_use_in_progress = 1;
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
    g_use_thread = CreateThread(NULL, 0, use_item_thread_proc, NULL, 0, NULL);
#else
    pthread_mutex_unlock(&g_use_lock);
    pthread_create(&g_use_thread, NULL, use_item_thread_proc, NULL);
#endif
}

int ogengine_sync_use_item_get_result(int* success_out, char* error_msg_buf, size_t error_msg_size) {
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#else
    pthread_mutex_lock(&g_use_lock);
#endif
    if (!g_use_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_use_lock);
#else
        pthread_mutex_unlock(&g_use_lock);
#endif
        return 0;
    }
    if (success_out) *success_out = g_use_success;
    if (error_msg_buf && error_msg_size) str_copy(error_msg_buf, g_use_error_msg, error_msg_size);
    g_use_has_result = 0;
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
#else
    pthread_mutex_unlock(&g_use_lock);
#endif
    return 1;
}

int ogengine_sync_use_item_in_progress(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#else
    pthread_mutex_lock(&g_use_lock);
#endif
    int in_progress = g_use_in_progress;
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
#else
    pthread_mutex_unlock(&g_use_lock);
#endif
    return in_progress;
}

#ifdef _WIN32
static DWORD WINAPI send_item_thread_proc(LPVOID param) {
#else
static void* send_item_thread_proc(void* param) {
#endif
    char target[SEND_TARGET_SIZE], item_name[SEND_ITEM_NAME_SIZE], item_id[SEND_ITEM_ID_SIZE];
    int qty, to_clan;
    ogengine_result_t res = OGENGINE_ERROR_NOT_INITIALIZED;
    const char* err = NULL;

    (void)param;
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#endif
#ifndef _WIN32
    pthread_mutex_lock(&g_send_lock);
#endif
    str_copy(target, g_send_target_buf, sizeof(target));
    str_copy(item_name, g_send_item_name_buf, sizeof(item_name));
    str_copy(item_id, g_send_item_id_buf, sizeof(item_id));
    qty = g_send_quantity;
    to_clan = g_send_to_clan;
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
#else
    pthread_mutex_unlock(&g_send_lock);
#endif

    if (qty < 1) qty = 1;
    if (to_clan)
        res = ogengine_send_item_to_clan(target, item_name, qty, item_id[0] ? item_id : NULL);
    else
        res = ogengine_send_item_to_avatar(target, item_name, qty, item_id[0] ? item_id : NULL);

    if (res != OGENGINE_SUCCESS)
        err = ogengine_get_last_error();

#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    g_send_in_progress = 0;
    g_send_has_result = 1;
    g_send_success = (res == OGENGINE_SUCCESS) ? 1 : 0;
    str_copy(g_send_error_msg, err ? err : "", sizeof(g_send_error_msg));
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
    return 0;
#else
    pthread_mutex_unlock(&g_send_lock);
    return NULL;
#endif
}

void ogengine_sync_send_item_start(const char* target, const char* item_name, int quantity, int to_clan, const char* item_id, ogengine_sync_send_item_on_done_fn on_done, void* user_data) {
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    if (g_send_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_send_lock);
#else
        pthread_mutex_unlock(&g_send_lock);
#endif
        return;
    }
    g_send_has_result = 0;
    g_send_on_done = on_done;
    g_send_on_done_user = user_data;
    str_copy(g_send_target_buf, target ? target : "", sizeof(g_send_target_buf));
    str_copy(g_send_item_name_buf, item_name ? item_name : "", sizeof(g_send_item_name_buf));
    str_copy(g_send_item_id_buf, item_id && item_id[0] ? item_id : "", sizeof(g_send_item_id_buf));
    g_send_quantity = quantity < 1 ? 1 : quantity;
    g_send_to_clan = to_clan ? 1 : 0;
    g_send_in_progress = 1;
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
    g_send_thread = CreateThread(NULL, 0, send_item_thread_proc, NULL, 0, NULL);
#else
    pthread_mutex_unlock(&g_send_lock);
    pthread_create(&g_send_thread, NULL, send_item_thread_proc, NULL);
#endif
}

int ogengine_sync_send_item_poll(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    if (g_send_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_send_lock);
#else
        pthread_mutex_unlock(&g_send_lock);
#endif
        return 0;
    }
    if (g_send_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_send_lock);
#else
        pthread_mutex_unlock(&g_send_lock);
#endif
        return 1;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
#else
    pthread_mutex_unlock(&g_send_lock);
#endif
    return -1;
}

int ogengine_sync_send_item_get_result(int* success_out, char* error_msg_buf, size_t error_msg_size) {
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    if (!g_send_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_send_lock);
#else
        pthread_mutex_unlock(&g_send_lock);
#endif
        return 0;
    }
    if (success_out) *success_out = g_send_success;
    if (error_msg_buf && error_msg_size) str_copy(error_msg_buf, g_send_error_msg, error_msg_size);
    g_send_has_result = 0;
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
#else
    pthread_mutex_unlock(&g_send_lock);
#endif
    return 1;
}

int ogengine_sync_send_item_in_progress(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    int in_progress = g_send_in_progress;
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
#else
    pthread_mutex_unlock(&g_send_lock);
#endif
    return in_progress;
}

void ogengine_sync_init(void) {
    if (g_sync_initialized) return;
#ifdef _WIN32
    InitializeCriticalSection(&g_auth_lock);
    InitializeCriticalSection(&g_inv_lock);
    InitializeCriticalSection(&g_send_lock);
    InitializeCriticalSection(&g_use_lock);
#endif
    g_sync_initialized = 1;
}

void ogengine_sync_cleanup(void) {
    if (!g_sync_initialized) return;
    ogengine_sync_inventory_clear_result();
#ifdef _WIN32
    DeleteCriticalSection(&g_use_lock);
    DeleteCriticalSection(&g_send_lock);
    DeleteCriticalSection(&g_inv_lock);
    DeleteCriticalSection(&g_auth_lock);
#endif
    g_sync_initialized = 0;
}

void ogengine_sync_set_add_item_log_cb(ogengine_sync_add_item_log_fn cb, void* user_data) {
    g_add_item_log_cb = cb;
    g_add_item_log_user = user_data;
}

/** Run pending completion callbacks on the main thread. Call once per frame. */
void ogengine_sync_pump(void) {
    ogengine_sync_auth_on_done_fn auth_fn = NULL;
    void* auth_ud = NULL;
#ifdef _WIN32
    EnterCriticalSection(&g_auth_lock);
#else
    pthread_mutex_lock(&g_auth_lock);
#endif
    if (g_auth_has_result && g_auth_on_done) {
        auth_fn = g_auth_on_done;
        auth_ud = g_auth_on_done_user;
        g_auth_on_done = NULL;
        g_auth_on_done_user = NULL;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_auth_lock);
#else
    pthread_mutex_unlock(&g_auth_lock);
#endif
    if (auth_fn)
        auth_fn(auth_ud);

    ogengine_sync_inventory_on_done_fn inv_fn = NULL;
    void* inv_ud = NULL;
    int add_log_count = 0;
    int add_log_success = 0;
    char add_log_names[ADD_ITEM_LOG_NAMES_MAX][ADD_ITEM_LOG_NAME_SIZE];
    char add_log_error[INV_ERROR_SIZE] = {0};
    ogengine_sync_add_item_log_fn add_log_cb = g_add_item_log_cb;
    void* add_log_ud = g_add_item_log_user;
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    if (g_inv_has_result && g_inv_on_done) {
        inv_fn = g_inv_on_done;
        inv_ud = g_inv_on_done_user;
        g_inv_on_done = NULL;
        g_inv_on_done_user = NULL;
        add_log_count = g_inv_add_item_log_count;
        add_log_success = g_inv_add_item_log_success;
        if (add_log_count > ADD_ITEM_LOG_NAMES_MAX) add_log_count = ADD_ITEM_LOG_NAMES_MAX;
        for (int i = 0; i < add_log_count; i++) {
            str_copy(add_log_names[i], g_inv_add_item_log_names[i], ADD_ITEM_LOG_NAME_SIZE);
        }
        str_copy(add_log_error, g_inv_add_item_log_error, sizeof(add_log_error));
        g_inv_add_item_log_count = 0;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
    if (inv_fn)
        inv_fn(inv_ud);
    if (add_log_cb && add_log_count > 0) {
        int i;
        for (i = 0; i < add_log_count; i++)
            add_log_cb(add_log_names[i], add_log_success, add_log_error, add_log_ud);
    }

    ogengine_sync_send_item_on_done_fn send_fn = NULL;
    void* send_ud = NULL;
#ifdef _WIN32
    EnterCriticalSection(&g_send_lock);
#else
    pthread_mutex_lock(&g_send_lock);
#endif
    if (g_send_has_result && g_send_on_done) {
        send_fn = g_send_on_done;
        send_ud = g_send_on_done_user;
        g_send_on_done = NULL;
        g_send_on_done_user = NULL;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_send_lock);
#else
    pthread_mutex_unlock(&g_send_lock);
#endif
    if (send_fn)
        send_fn(send_ud);

    ogengine_sync_use_item_on_done_fn use_fn = NULL;
    void* use_ud = NULL;
#ifdef _WIN32
    EnterCriticalSection(&g_use_lock);
#else
    pthread_mutex_lock(&g_use_lock);
#endif
    if (g_use_has_result && g_use_on_done) {
        use_fn = g_use_on_done;
        use_ud = g_use_on_done_user;
        g_use_on_done = NULL;
        g_use_on_done_user = NULL;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_use_lock);
#else
    pthread_mutex_unlock(&g_use_lock);
#endif
    if (use_fn)
        use_fn(use_ud);
}

#ifdef _WIN32
static DWORD WINAPI inventory_thread_proc(LPVOID param) {
#else
static void* inventory_thread_proc(void* param) {
#endif
    ogengine_sync_local_item_t* local;
    int local_count;
    char default_src[64];
    ogengine_item_list_t* list = NULL;
    ogengine_result_t result = OGENGINE_ERROR_NOT_INITIALIZED;
    const char* err = NULL;
    char logged_names[ADD_ITEM_LOG_NAMES_MAX][ADD_ITEM_LOG_NAME_SIZE];
    int logged_count = 0;

    (void)param;
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#endif
#ifndef _WIN32
    pthread_mutex_lock(&g_inv_lock);
#endif
    local = g_inv_local_items;
    local_count = g_inv_local_count;
    str_copy(default_src, g_inv_default_game_source, sizeof(default_src));
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif

    /* Queue each add then flush (batching). Stack: name ends with _NNNNNN (e.g. Shells_000001) – send base name, API increments Quantity. Unlock: has_item first, queue add only if not present. */
    g_inv_add_item_error[0] = '\0';
    if (local && local_count > 0 && default_src[0]) {
        int i;
        for (i = 0; i < local_count; i++) {
            if (local[i].synced) continue;
            {
                const char* n = local[i].name;
                size_t len = strlen(n);
                int is_stack_event = 0;
                if (len >= 8 && n[len - 7] == '_') {
                    int j;
                    is_stack_event = 1;
                    for (j = 0; j < 6; j++)
                        if (n[len - 6 + j] < '0' || n[len - 6 + j] > '9') { is_stack_event = 0; break; }
                }
                if (is_stack_event) {
                    /* Send base name (no _NNNNNN) so API increments Quantity on existing item. */
                    char base_name[128];
                    size_t base_len = len >= 8 ? (size_t)(len - 7) : len;
                    if (base_len >= sizeof(base_name)) base_len = sizeof(base_name) - 1;
                    memcpy(base_name, n, base_len);
                    base_name[base_len] = '\0';
                    const char* nft = (local[i].nft_id[0] != '\0') ? local[i].nft_id : NULL;
                    ogengine_queue_add_item(
                        base_name,
                        local[i].description,
                        local[i].game_source[0] ? local[i].game_source : default_src,
                        local[i].item_type[0] ? local[i].item_type : "KeyItem",
                        nft, 1, 1);
                    if (logged_count < ADD_ITEM_LOG_NAMES_MAX)
                        str_copy(logged_names[logged_count++], base_name, ADD_ITEM_LOG_NAME_SIZE);
                } else {
                    if (!ogengine_has_item(local[i].name)) {
                        const char* nft = (local[i].nft_id[0] != '\0') ? local[i].nft_id : NULL;
                        ogengine_queue_add_item(
                            local[i].name,
                            local[i].description,
                            local[i].game_source[0] ? local[i].game_source : default_src,
                            local[i].item_type[0] ? local[i].item_type : "KeyItem",
                            nft, 1, 1);
                        if (logged_count < ADD_ITEM_LOG_NAMES_MAX)
                            str_copy(logged_names[logged_count++], local[i].name, ADD_ITEM_LOG_NAME_SIZE);
                    }
                }
                local[i].synced = 1;
            }
        }
        ogengine_result_t flush_res = ogengine_flush_add_item_jobs();
        if (flush_res != OGENGINE_SUCCESS && g_inv_add_item_error[0] == '\0') {
            const char* flush_err = ogengine_get_last_error();
            str_copy(g_inv_add_item_error, flush_err ? flush_err : "flush add_item jobs failed", sizeof(g_inv_add_item_error));
        }
    }

    result = ogengine_get_inventory(&list);
    if (result != OGENGINE_SUCCESS) {
        err = ogengine_get_last_error();
        if (!err || !err[0]) err = "Unknown error";
    } else if (!list) {
        result = OGENGINE_ERROR_API_ERROR;
        err = "Inventory API returned success but no data";
    }

#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    g_inv_in_progress = 0;
    g_inv_has_result = 1;
    if (g_inv_list)
        ogengine_free_item_list(g_inv_list);
    g_inv_list = list;
    g_inv_result = result;
    str_copy(g_inv_error_msg, err ? err : "", sizeof(g_inv_error_msg));
    /* If add_item failed (e.g. not logged in / no avatar), surface that so user sees why pickups aren't saved */
    if (g_inv_add_item_error[0] != '\0') {
        str_copy(g_inv_error_msg, g_inv_add_item_error, sizeof(g_inv_error_msg));
        if (g_inv_result == OGENGINE_SUCCESS)
            g_inv_result = OGENGINE_ERROR_NOT_INITIALIZED; /* so UI shows error */
    }
    /* Pending add_item log for main-thread callback in ogengine_sync_pump() */
    g_inv_add_item_log_count = logged_count;
    g_inv_add_item_log_success = (g_inv_add_item_error[0] == '\0') ? 1 : 0;
    str_copy(g_inv_add_item_log_error, g_inv_add_item_error, sizeof(g_inv_add_item_log_error));
    {
        int k;
        for (k = 0; k < logged_count && k < ADD_ITEM_LOG_NAMES_MAX; k++)
            str_copy(g_inv_add_item_log_names[k], logged_names[k], ADD_ITEM_LOG_NAME_SIZE);
    }
    /* Callback is invoked from main thread in ogengine_sync_pump(), not from this worker. */
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
    return 0;
#else
    pthread_mutex_unlock(&g_inv_lock);
    return NULL;
#endif
}

void ogengine_sync_inventory_deliver_result(ogengine_item_list_t* list, ogengine_result_t result, const char* error_msg) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    g_inv_in_progress = 0;
    g_inv_has_result = 1;
    if (g_inv_list)
        ogengine_free_item_list(g_inv_list);
    g_inv_list = list;
    g_inv_result = result;
    str_copy(g_inv_error_msg, error_msg ? error_msg : "", sizeof(g_inv_error_msg));
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
}

void ogengine_sync_inventory_start(ogengine_sync_local_item_t* local_items,
    int local_count,
    const char* default_game_source,
    ogengine_sync_inventory_on_done_fn on_done,
    void* on_done_user) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    if (g_inv_in_progress) {
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
        return;
    }
    if (g_inv_list) {
        ogengine_free_item_list(g_inv_list);
        g_inv_list = NULL;
    }
    g_inv_has_result = 0;
    g_inv_local_items = local_items;
    g_inv_local_count = local_count < 0 ? 0 : local_count;
    str_copy(g_inv_default_game_source, default_game_source ? default_game_source : "", sizeof(g_inv_default_game_source));
    g_inv_on_done = on_done;
    g_inv_on_done_user = on_done_user;
    g_inv_in_progress = 1;
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
    g_inv_thread = CreateThread(NULL, 0, inventory_thread_proc, NULL, 0, NULL);
#else
    pthread_mutex_unlock(&g_inv_lock);
    pthread_create(&g_inv_thread, NULL, inventory_thread_proc, NULL);
#endif
}

int ogengine_sync_inventory_poll(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    if (g_inv_in_progress) {
#ifdef _WIN32
        LeaveCriticalSection(&g_inv_lock);
#else
        pthread_mutex_unlock(&g_inv_lock);
#endif
        return 0;
    }
    if (g_inv_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_inv_lock);
#else
        pthread_mutex_unlock(&g_inv_lock);
#endif
        return 1;
    }
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
    return -1;
}

int ogengine_sync_inventory_get_result(ogengine_item_list_t** list_out,
    ogengine_result_t* result_out,
    char* error_msg_buf, size_t error_msg_size) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    if (!g_inv_has_result) {
#ifdef _WIN32
        LeaveCriticalSection(&g_inv_lock);
#else
        pthread_mutex_unlock(&g_inv_lock);
#endif
        return 0;
    }
    if (list_out) *list_out = g_inv_list;
    if (result_out) *result_out = g_inv_result;
    if (error_msg_buf && error_msg_size) str_copy(error_msg_buf, g_inv_error_msg, error_msg_size);
    g_inv_has_result = 0;
    g_inv_list = NULL; /* ownership transferred to caller */
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
    return 1;
}

void ogengine_sync_inventory_clear_result(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    if (g_inv_list) {
        ogengine_free_item_list(g_inv_list);
        g_inv_list = NULL;
    }
    g_inv_has_result = 0;
    g_inv_add_item_error[0] = '\0';
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
}

int ogengine_sync_inventory_in_progress(void) {
#ifdef _WIN32
    EnterCriticalSection(&g_inv_lock);
#else
    pthread_mutex_lock(&g_inv_lock);
#endif
    int in_progress = g_inv_in_progress;
#ifdef _WIN32
    LeaveCriticalSection(&g_inv_lock);
#else
    pthread_mutex_unlock(&g_inv_lock);
#endif
    return in_progress;
}

ogengine_result_t ogengine_sync_single_item(const char* name,
    const char* description,
    const char* game_source,
    const char* item_type,
    const char* nft_id) {
    ogengine_result_t res;
    if (!name || !name[0]) return OGENGINE_ERROR_INVALID_PARAM;
    if (ogengine_has_item(name))
        return OGENGINE_SUCCESS;
    ogengine_queue_add_item(name, description ? description : "", game_source ? game_source : "", item_type ? item_type : "KeyItem", nft_id, 1, 1);
    res = ogengine_flush_add_item_jobs();
    if (g_add_item_log_cb) {
        int success = (res == OGENGINE_SUCCESS) ? 1 : 0;
        const char* err = (res != OGENGINE_SUCCESS) ? ogengine_get_last_error() : "";
        g_add_item_log_cb(name, success, err ? err : "", g_add_item_log_user);
    }
    return res;
}
