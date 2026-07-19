/**
 * ogamelib_json.h — OGameLib minimal JSON value extractor
 *
 * A single lightweight function that extracts a string value from a flat JSON
 * object by key name. Handles quoted strings, unquoted scalars (numbers, booleans),
 * and basic escape sequences. Does not require a full JSON parser.
 *
 * No dependencies beyond the C standard library.
 */
#ifndef OGAMELIB_JSON_H
#define OGAMELIB_JSON_H

#include <string.h>
#include <stdio.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Extract a JSON string/scalar value by key from a flat JSON object.
 *
 * Searches for "key" in json, then copies the value (quoted or unquoted)
 * into value_out (up to max_len - 1 bytes, always null-terminated).
 *
 * Handles escape sequences: \\, \", \n, \t, \r.
 * Works with both quoted string values ("hello") and unquoted scalars (42, true, null).
 *
 * @param json      Source JSON text (null-terminated).
 * @param key       Key name to search for (without quotes).
 * @param value_out Destination buffer.
 * @param max_len   Size of destination buffer (including null terminator).
 * @return          1 if the key was found and value copied, 0 otherwise.
 */
static inline int ogamelib_json_extract(const char* json, const char* key,
                                         char* value_out, int max_len)
{
    if (!json || !key || !value_out || max_len <= 0) return 0;
    value_out[0] = '\0';

    /* Build search pattern: "key" */
    char pattern[256];
    if (snprintf(pattern, sizeof(pattern), "\"%s\"", key) >= (int)sizeof(pattern))
        return 0;

    const char* pos = strstr(json, pattern);
    if (!pos) return 0;

    /* Skip past "key" and optional whitespace + colon */
    pos += strlen(pattern);
    while (*pos == ' ' || *pos == '\t' || *pos == '\r' || *pos == '\n') pos++;
    if (*pos != ':') return 0;
    pos++;
    while (*pos == ' ' || *pos == '\t' || *pos == '\r' || *pos == '\n') pos++;

    int out = 0;
    if (*pos == '"') {
        /* Quoted string value */
        pos++;
        while (*pos && *pos != '"' && out < max_len - 1) {
            if (*pos == '\\') {
                pos++;
                switch (*pos) {
                    case '"':  value_out[out++] = '"';  break;
                    case '\\': value_out[out++] = '\\'; break;
                    case 'n':  value_out[out++] = '\n'; break;
                    case 't':  value_out[out++] = '\t'; break;
                    case 'r':  value_out[out++] = '\r'; break;
                    default:   value_out[out++] = *pos; break;
                }
            } else {
                value_out[out++] = *pos;
            }
            pos++;
        }
    } else {
        /* Unquoted scalar (number, boolean, null) — read until delimiter */
        while (*pos && *pos != ',' && *pos != '}' && *pos != '\r' && *pos != '\n'
               && *pos != ' ' && *pos != '\t' && out < max_len - 1) {
            value_out[out++] = *pos++;
        }
    }
    value_out[out] = '\0';
    return out > 0 || *pos == '"'; /* key found (even empty string is valid) */
}

/**
 * Write a single JSON key/string-value line into buf.
 * Produces:  "key": "value"
 * Returns number of bytes written (excluding null terminator).
 */
static inline int ogamelib_json_write_kv(char* buf, size_t buf_size,
                                          const char* key, const char* value)
{
    if (!buf || buf_size == 0) return 0;
    return snprintf(buf, buf_size, "    \"%s\": \"%s\"", key, value ? value : "");
}

#ifdef __cplusplus
extern "C" {
#endif

#endif /* OGAMELIB_JSON_H */
