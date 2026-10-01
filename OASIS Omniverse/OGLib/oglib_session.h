/**
 * oglib_session.h — OGLib runtime session forwarders
 *
 * The STAR API ships as a native DLL (ogengine.dll / libOGEngineClient.so) and exports
 * functions like ogengine_authenticate_with_jwt_out, ogengine_set_saved_session, etc.
 * via [UnmanagedCallersOnly] from the C# StarApiClient.
 *
 * These session functions are declared in ogengine.h but the linker cannot always
 * resolve them at link time (e.g. when the game links ogengine.lib but the DLL is
 * loaded at runtime by the engine rather than the stub). This file provides a
 * runtime-forwarding shim layer that uses GetProcAddress (Win32) / dlsym (POSIX)
 * to resolve each symbol at first call.
 *
 * USAGE
 * -----
 * In exactly ONE .c/.cpp file in your game project, before including this header:
 *
 *   #define OGLIB_SESSION_IMPL
 *   #include "oglib_session.h"
 *
 * All other files that need the declarations just include it without the define.
 *
 * The #define must appear in a .c/.cpp that is also including ogengine.h (or a
 * translation unit that links ogengine.lib), so the linker sees the definitions.
 */
#ifndef OGLIB_SESSION_H
#define OGLIB_SESSION_H

#include "ogengine.h"  /* ogengine_result_t, OGENGINE_ERROR_NOT_INITIALIZED */
#include <stddef.h>    /* size_t */

#ifdef __cplusplus
extern "C" {
#endif

/* ── Forward declarations (available everywhere that includes this header) ── */

ogengine_result_t ogengine_authenticate_with_jwt_out(const char* username,
    const char* password, char* jwt_buf, size_t jwt_size);

ogengine_result_t ogengine_set_saved_session(const char* jwt);

ogengine_result_t ogengine_restore_session(void);

int  ogengine_get_current_username(char* buf, size_t buf_size);
int  ogengine_get_current_jwt(char* buf, size_t buf_size);
void ogengine_set_refresh_token(const char* refresh_token);
int  ogengine_get_current_refresh_token(char* buf, size_t buf_size);
int  ogengine_is_session_expired(void);
void ogengine_request_inventory_in_background(void);

/* ── Implementation (compiled once, in the TU that defines OGLIB_SESSION_IMPL) ── */

#ifdef OGLIB_SESSION_IMPL

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define OGLIB_LOAD_FN(ret, name, ...) \
    typedef ret (__cdecl *_ogame_##name##_t)(__VA_ARGS__); \
    static _ogame_##name##_t _ogame_##name##_fn = NULL; \
    if (!_ogame_##name##_fn) { \
        HMODULE h = GetModuleHandleA("ogengine.dll"); \
        if (h) _ogame_##name##_fn = (_ogame_##name##_t)(void*)GetProcAddress(h, #name); \
    }

ogengine_result_t ogengine_authenticate_with_jwt_out(const char* u, const char* p, char* buf, size_t sz) {
    OGLIB_LOAD_FN(ogengine_result_t, ogengine_authenticate_with_jwt_out, const char*, const char*, char*, size_t)
    return _ogame_ogengine_authenticate_with_jwt_out_fn
        ? _ogame_ogengine_authenticate_with_jwt_out_fn(u, p, buf, sz)
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
ogengine_result_t ogengine_set_saved_session(const char* jwt) {
    OGLIB_LOAD_FN(ogengine_result_t, ogengine_set_saved_session, const char*)
    return _ogame_ogengine_set_saved_session_fn
        ? _ogame_ogengine_set_saved_session_fn(jwt)
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
ogengine_result_t ogengine_restore_session(void) {
    OGLIB_LOAD_FN(ogengine_result_t, ogengine_restore_session, void)
    return _ogame_ogengine_restore_session_fn
        ? _ogame_ogengine_restore_session_fn()
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
int ogengine_get_current_username(char* buf, size_t sz) {
    OGLIB_LOAD_FN(int, ogengine_get_current_username, char*, size_t)
    return _ogame_ogengine_get_current_username_fn
        ? _ogame_ogengine_get_current_username_fn(buf, sz) : 0;
}
int ogengine_get_current_jwt(char* buf, size_t sz) {
    OGLIB_LOAD_FN(int, ogengine_get_current_jwt, char*, size_t)
    return _ogame_ogengine_get_current_jwt_fn
        ? _ogame_ogengine_get_current_jwt_fn(buf, sz) : 0;
}
void ogengine_set_refresh_token(const char* tok) {
    OGLIB_LOAD_FN(void, ogengine_set_refresh_token, const char*)
    if (_ogame_ogengine_set_refresh_token_fn) _ogame_ogengine_set_refresh_token_fn(tok);
}
int ogengine_get_current_refresh_token(char* buf, size_t sz) {
    OGLIB_LOAD_FN(int, ogengine_get_current_refresh_token, char*, size_t)
    return _ogame_ogengine_get_current_refresh_token_fn
        ? _ogame_ogengine_get_current_refresh_token_fn(buf, sz) : 0;
}
int ogengine_is_session_expired(void) {
    OGLIB_LOAD_FN(int, ogengine_is_session_expired, void)
    return _ogame_ogengine_is_session_expired_fn
        ? _ogame_ogengine_is_session_expired_fn() : 0;
}
void ogengine_request_inventory_in_background(void) {
    OGLIB_LOAD_FN(void, ogengine_request_inventory_in_background, void)
    if (_ogame_ogengine_request_inventory_in_background_fn)
        _ogame_ogengine_request_inventory_in_background_fn();
}

#undef OGLIB_LOAD_FN

#else /* POSIX */
#include <dlfcn.h>

/* Try RTLD_NOLOAD first (already loaded), fall back to dlopen(NULL) for in-process. */
static void* oglib_session_handle(void) {
    void* h = dlopen("libOGEngineClient.so", RTLD_NOW | RTLD_NOLOAD);
    if (!h) h = dlopen(NULL, RTLD_NOW);
    return h;
}

#define OGLIB_LOAD_FN_POSIX(ret, name, ...) \
    typedef ret (*_ogame_##name##_t)(__VA_ARGS__); \
    static _ogame_##name##_t _ogame_##name##_fn = NULL; \
    if (!_ogame_##name##_fn) { \
        void* h = oglib_session_handle(); \
        if (h) _ogame_##name##_fn = (_ogame_##name##_t)dlsym(h, #name); \
    }

ogengine_result_t ogengine_authenticate_with_jwt_out(const char* u, const char* p, char* buf, size_t sz) {
    OGLIB_LOAD_FN_POSIX(ogengine_result_t, ogengine_authenticate_with_jwt_out, const char*, const char*, char*, size_t)
    return _ogame_ogengine_authenticate_with_jwt_out_fn
        ? _ogame_ogengine_authenticate_with_jwt_out_fn(u, p, buf, sz)
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
ogengine_result_t ogengine_set_saved_session(const char* jwt) {
    OGLIB_LOAD_FN_POSIX(ogengine_result_t, ogengine_set_saved_session, const char*)
    return _ogame_ogengine_set_saved_session_fn
        ? _ogame_ogengine_set_saved_session_fn(jwt)
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
ogengine_result_t ogengine_restore_session(void) {
    OGLIB_LOAD_FN_POSIX(ogengine_result_t, ogengine_restore_session, void)
    return _ogame_ogengine_restore_session_fn
        ? _ogame_ogengine_restore_session_fn()
        : (ogengine_result_t)OGENGINE_ERROR_NOT_INITIALIZED;
}
int ogengine_get_current_username(char* buf, size_t sz) {
    OGLIB_LOAD_FN_POSIX(int, ogengine_get_current_username, char*, size_t)
    return _ogame_ogengine_get_current_username_fn
        ? _ogame_ogengine_get_current_username_fn(buf, sz) : 0;
}
int ogengine_get_current_jwt(char* buf, size_t sz) {
    OGLIB_LOAD_FN_POSIX(int, ogengine_get_current_jwt, char*, size_t)
    return _ogame_ogengine_get_current_jwt_fn
        ? _ogame_ogengine_get_current_jwt_fn(buf, sz) : 0;
}
void ogengine_set_refresh_token(const char* tok) {
    OGLIB_LOAD_FN_POSIX(void, ogengine_set_refresh_token, const char*)
    if (_ogame_ogengine_set_refresh_token_fn) _ogame_ogengine_set_refresh_token_fn(tok);
}
int ogengine_get_current_refresh_token(char* buf, size_t sz) {
    OGLIB_LOAD_FN_POSIX(int, ogengine_get_current_refresh_token, char*, size_t)
    return _ogame_ogengine_get_current_refresh_token_fn
        ? _ogame_ogengine_get_current_refresh_token_fn(buf, sz) : 0;
}
int ogengine_is_session_expired(void) {
    OGLIB_LOAD_FN_POSIX(int, ogengine_is_session_expired, void)
    return _ogame_ogengine_is_session_expired_fn
        ? _ogame_ogengine_is_session_expired_fn() : 0;
}
void ogengine_request_inventory_in_background(void) {
    OGLIB_LOAD_FN_POSIX(void, ogengine_request_inventory_in_background, void)
    if (_ogame_ogengine_request_inventory_in_background_fn)
        _ogame_ogengine_request_inventory_in_background_fn();
}

#undef OGLIB_LOAD_FN_POSIX

#endif /* _WIN32 / POSIX */
#endif /* OGLIB_SESSION_IMPL */

#ifdef __cplusplus
}
#endif

#endif /* OGLIB_SESSION_H */
