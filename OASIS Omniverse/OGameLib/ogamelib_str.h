/**
 * ogamelib_str.h — OGameLib string utilities
 *
 * Lightweight string helpers needed by every C game integration.
 * No dependencies beyond the C standard library.
 */
#ifndef OGAMELIB_STR_H
#define OGAMELIB_STR_H

#include <string.h>
#include <ctype.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Case-insensitive substring search.
 * Returns 1 if needle is found anywhere in haystack (case-insensitive), 0 otherwise.
 */
static inline int ogamelib_str_contains_nocase(const char* haystack, const char* needle)
{
    if (!haystack || !needle || !*needle) return 0;
    size_t nlen = strlen(needle);
    size_t hlen = strlen(haystack);
    if (nlen > hlen) return 0;
    for (size_t i = 0; i <= hlen - nlen; i++) {
        size_t j;
        for (j = 0; j < nlen; j++) {
            if (tolower((unsigned char)haystack[i + j]) != tolower((unsigned char)needle[j]))
                break;
        }
        if (j == nlen) return 1;
    }
    return 0;
}

/**
 * Safe string copy: always null-terminates dest even when src is longer than dest_size.
 * Returns number of characters written (excluding null terminator).
 */
static inline size_t ogamelib_str_copy(char* dest, const char* src, size_t dest_size)
{
    if (!dest || dest_size == 0) return 0;
    if (!src) { dest[0] = '\0'; return 0; }
    size_t i = 0;
    while (i < dest_size - 1 && src[i]) { dest[i] = src[i]; i++; }
    dest[i] = '\0';
    return i;
}

/**
 * Trim leading and trailing whitespace in-place.
 * Returns pointer to first non-whitespace character (within the original buffer).
 */
static inline char* ogamelib_str_trim(char* s)
{
    if (!s) return s;
    while (*s && isspace((unsigned char)*s)) s++;
    char* end = s + strlen(s);
    while (end > s && isspace((unsigned char)*(end - 1))) end--;
    *end = '\0';
    return s;
}

#ifdef __cplusplus
}
#endif

#endif /* OGAMELIB_STR_H */
