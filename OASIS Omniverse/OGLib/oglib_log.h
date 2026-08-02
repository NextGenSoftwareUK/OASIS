/**
 * oglib_log.h — lightweight logging for OGLib / game integrations
 *
 * Usage:
 *   oglib_log(OGLIB_LOG_INFO,  "Item picked up: %s", item_name);
 *   oglib_log(OGLIB_LOG_WARN,  "Poll returned unexpected value %d", rc);
 *   oglib_log(OGLIB_LOG_ERROR, "ogengine_init failed: %s", ogengine_get_last_error());
 *
 * By default output goes to stderr. Override before including oglib.h:
 *   #define OGLIB_LOG_SINK(level, msg)  MyEngineLog(msg)
 */
#ifndef OGLIB_LOG_H
#define OGLIB_LOG_H

#include <stdio.h>
#include <stdarg.h>

typedef enum {
    OGLIB_LOG_DEBUG = 0,
    OGLIB_LOG_INFO  = 1,
    OGLIB_LOG_WARN  = 2,
    OGLIB_LOG_ERROR = 3
} oglib_log_level_t;

static const char* const _oglib_level_str[] = { "DEBUG", "INFO", "WARN", "ERROR" };

#ifndef OGLIB_LOG_MIN_LEVEL
#  define OGLIB_LOG_MIN_LEVEL OGLIB_LOG_INFO
#endif

#ifndef OGLIB_LOG_SINK
#  define OGLIB_LOG_SINK(level, msg) fprintf(stderr, "[OASIS/%s] %s\n", _oglib_level_str[level], (msg))
#endif

static void oglib_log(oglib_log_level_t level, const char* fmt, ...)
#if defined(__GNUC__) || defined(__clang__)
    __attribute__((format(printf, 2, 3)))
#endif
    ;

static void oglib_log(oglib_log_level_t level, const char* fmt, ...)
{
    char _oglib_buf[1024];
    va_list ap;
    if (level < OGLIB_LOG_MIN_LEVEL) return;
    va_start(ap, fmt);
    vsnprintf(_oglib_buf, sizeof(_oglib_buf), fmt, ap);
    va_end(ap);
    OGLIB_LOG_SINK(level, _oglib_buf);
}

#endif /* OGLIB_LOG_H */
