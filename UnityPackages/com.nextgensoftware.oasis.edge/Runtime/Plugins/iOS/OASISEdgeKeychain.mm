#import <Foundation/Foundation.h>
#import <Security/Security.h>

static NSMutableDictionary *OasisQuery(NSString *key) {
    return [@{ (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
               (__bridge id)kSecAttrService: @"one.oasisomniverse.edge",
               (__bridge id)kSecAttrAccount: key } mutableCopy];
}

extern "C" int OasisEdgeKeychainSave(const char *key, const char *value) {
    if (!key || !value) return errSecParam;
    NSString *account = [NSString stringWithUTF8String:key];
    NSData *data = [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding];
    NSMutableDictionary *query = OasisQuery(account);
    SecItemDelete((__bridge CFDictionaryRef)query);
    query[(__bridge id)kSecValueData] = data;
    query[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    return (int)SecItemAdd((__bridge CFDictionaryRef)query, NULL);
}

extern "C" char *OasisEdgeKeychainLoad(const char *key) {
    if (!key) return NULL;
    NSMutableDictionary *query = OasisQuery([NSString stringWithUTF8String:key]);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
    if (status == errSecItemNotFound) return NULL;
    if (status != errSecSuccess || !result) return NULL;
    NSData *data = (__bridge_transfer NSData *)result;
    NSString *value = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    return value ? strdup([value UTF8String]) : NULL;
}

extern "C" int OasisEdgeKeychainDelete(const char *key) {
    if (!key) return errSecParam;
    OSStatus status = SecItemDelete((__bridge CFDictionaryRef)OasisQuery([NSString stringWithUTF8String:key]));
    return status == errSecItemNotFound ? errSecSuccess : (int)status;
}

extern "C" void OasisEdgeKeychainFree(char *value) { if (value) free(value); }
