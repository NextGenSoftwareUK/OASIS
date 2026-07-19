/**
 * ogamelib_session.h — OGameLib runtime session forwarders
 *
 * The STAR API ships as a native DLL (star_api.dll / libstar_api.so) and exports
 * functions like star_api_authenticate_with_jwt_out, star_api_set_saved_session, etc.
 * via [UnmanagedCallersOnly] from the C# StarApiClient.
 *
 * These session functions are declared in star_api.h but the linker cannot always
 * resolve them at link time (e.g. when the game links star_api.lib but the DLL is
 * loaded at runtime by the engine rather than the stub). This file provides a
 * runtime-forwarding shim layer that uses GetProcAddress (Win32) / dlsym (POSIX)
 * to resolve each symbol at first call.
 *
 * USAGE
 * -----
 * In exactly ONE .c/.cpp file in your game project, before including this header:
 *
 *   #define OGAMELIB_SESSION_IMPL
 *   #include "ogamelib_session.h"
 *
 * All other files that need the declarations just include it without the define.
 *
 * The #define must appear in a .c/.cpp that is also including star_api.h (or a
 * translation unit that links star_api.lib), so the linker sees the definitions.
 */
#ifndef OGAMELIB_SESSION_H
#define OGAMELIB_SESSION_H

#include "star_api.h"  /* star_api_result_t, STAR_API_ERROR_NOT_INITIALIZED */
#include <stddef.h>    /* size_t */

#ifdef __cplusplus
extern "C" {
#endif

/* ── Forward declarations (available everywhere that includes this header) ── */

star_api_result_t star_api_authenticate_with_jwt_out(const char* username,
    const char* password, char* jwt_buf, size_t jwt_size);

star_api_result_t star_api_set_saved_session(const char* jwt);

star_api_result_t star_api_restore_session(void);

int  star_api_get_current_username(char* buf, size_t buf_size);
int  star_api_get_current_jwt(char* buf, size_t buf_size);
void star_api_set_refresh_token(const char* refresh_token);
int  star_api_get_current_refresh_token(char* buf, size_t buf_size);
int  star_api_is_session_expired(void);
void star_api_request_inventory_in_background(void);

/* ── Implementation (compiled once, in the TU that defines OGAMELIB_SESSION_IMPL) ── */

#ifdef OGAMELIB_SESSION_IMPL

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define OGAMELIB_LOAD_FN(ret, name, ...) \
    typedef ret (__cdecl *_ogame_##name##_t)(__VA_ARGS__); \
    static _ogame_##name##_t _ogame_##name##_fn = NULL; \
    if (!_ogame_##name##_fn) { \
        HMODULE h = GetModuleHandleA("star_api.dll"); \
        if (h) _ogame_##name##_fn = (_ogame_##name##_t)(void*)GetProcAddress(h, #name); \
    }

star_api_result_t star_api_authenticate_with_jwt_out(const char* u, const char* p, char* buf, size_t sz) {
    OGAMELIB_LOAD_FN(star_api_result_t, star_api_authenticate_with_jwt_out, const char*, const char*, char*, size_t)
    return _ogame_star_api_authenticate_with_jwt_out_fn
        ? _ogame_star_api_authenticate_with_jwt_out_fn(u, p, buf, sz)
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_saved_session(const char* jwt) {
    OGAMELIB_LOAD_FN(star_api_result_t, star_api_set_saved_session, const char*)
    return _ogame_star_api_set_saved_session_fn
        ? _ogame_star_api_set_saved_session_fn(jwt)
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_restore_session(void) {
    OGAMELIB_LOAD_FN(star_api_result_t, star_api_restore_session, void)
    return _ogame_star_api_restore_session_fn
        ? _ogame_star_api_restore_session_fn()
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
int star_api_get_current_username(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN(int, star_api_get_current_username, char*, size_t)
    return _ogame_star_api_get_current_username_fn
        ? _ogame_star_api_get_current_username_fn(buf, sz) : 0;
}
int star_api_get_current_jwt(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN(int, star_api_get_current_jwt, char*, size_t)
    return _ogame_star_api_get_current_jwt_fn
        ? _ogame_star_api_get_current_jwt_fn(buf, sz) : 0;
}
void star_api_set_refresh_token(const char* tok) {
    OGAMELIB_LOAD_FN(void, star_api_set_refresh_token, const char*)
    if (_ogame_star_api_set_refresh_token_fn) _ogame_star_api_set_refresh_token_fn(tok);
}
int star_api_get_current_refresh_token(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN(int, star_api_get_current_refresh_token, char*, size_t)
    return _ogame_star_api_get_current_refresh_token_fn
        ? _ogame_star_api_get_current_refresh_token_fn(buf, sz) : 0;
}
int star_api_is_session_expired(void) {
    OGAMELIB_LOAD_FN(int, star_api_is_session_expired, void)
    return _ogame_star_api_is_session_expired_fn
        ? _ogame_star_api_is_session_expired_fn() : 0;
}
void star_api_request_inventory_in_background(void) {
    OGAMELIB_LOAD_FN(void, star_api_request_inventory_in_background, void)
    if (_ogame_star_api_request_inventory_in_background_fn)
        _ogame_star_api_request_inventory_in_background_fn();
}

#undef OGAMELIB_LOAD_FN

#else /* POSIX */
#include <dlfcn.h>

/* Try RTLD_NOLOAD first (already loaded), fall back to dlopen(NULL) for in-process. */
static void* ogamelib_session_handle(void) {
    void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
    if (!h) h = dlopen(NULL, RTLD_NOW);
    return h;
}

#define OGAMELIB_LOAD_FN_POSIX(ret, name, ...) \
    typedef ret (*_ogame_##name##_t)(__VA_ARGS__); \
    static _ogame_##name##_t _ogame_##name##_fn = NULL; \
    if (!_ogame_##name##_fn) { \
        void* h = ogamelib_session_handle(); \
        if (h) _ogame_##name##_fn = (_ogame_##name##_t)dlsym(h, #name); \
    }

star_api_result_t star_api_authenticate_with_jwt_out(const char* u, const char* p, char* buf, size_t sz) {
    OGAMELIB_LOAD_FN_POSIX(star_api_result_t, star_api_authenticate_with_jwt_out, const char*, const char*, char*, size_t)
    return _ogame_star_api_authenticate_with_jwt_out_fn
        ? _ogame_star_api_authenticate_with_jwt_out_fn(u, p, buf, sz)
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_saved_session(const char* jwt) {
    OGAMELIB_LOAD_FN_POSIX(star_api_result_t, star_api_set_saved_session, const char*)
    return _ogame_star_api_set_saved_session_fn
        ? _ogame_star_api_set_saved_session_fn(jwt)
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_restore_session(void) {
    OGAMELIB_LOAD_FN_POSIX(star_api_result_t, star_api_restore_session, void)
    return _ogame_star_api_restore_session_fn
        ? _ogame_star_api_restore_session_fn()
        : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
int star_api_get_current_username(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN_POSIX(int, star_api_get_current_username, char*, size_t)
    return _ogame_star_api_get_current_username_fn
        ? _ogame_star_api_get_current_username_fn(buf, sz) : 0;
}
int star_api_get_current_jwt(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN_POSIX(int, star_api_get_current_jwt, char*, size_t)
    return _ogame_star_api_get_current_jwt_fn
        ? _ogame_star_api_get_current_jwt_fn(buf, sz) : 0;
}
void star_api_set_refresh_token(const char* tok) {
    OGAMELIB_LOAD_FN_POSIX(void, star_api_set_refresh_token, const char*)
    if (_ogame_star_api_set_refresh_token_fn) _ogame_star_api_set_refresh_token_fn(tok);
}
int star_api_get_current_refresh_token(char* buf, size_t sz) {
    OGAMELIB_LOAD_FN_POSIX(int, star_api_get_current_refresh_token, char*, size_t)
    return _ogame_star_api_get_current_refresh_token_fn
        ? _ogame_star_api_get_current_refresh_token_fn(buf, sz) : 0;
}
int star_api_is_session_expired(void) {
    OGAMELIB_LOAD_FN_POSIX(int, star_api_is_session_expired, void)
    return _ogame_star_api_is_session_expired_fn
        ? _ogame_star_api_is_session_expired_fn() : 0;
}
void star_api_request_inventory_in_background(void) {
    OGAMELIB_LOAD_FN_POSIX(void, star_api_request_inventory_in_background, void)
    if (_ogame_star_api_request_inventory_in_background_fn)
        _ogame_star_api_request_inventory_in_background_fn();
}

#undef OGAMELIB_LOAD_FN_POSIX

#endif /* _WIN32 / POSIX */
#endif /* OGAMELIB_SESSION_IMPL */

#ifdef __cplusplus
}
#endif

#endif /* OGAMELIB_SESSION_H */
