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
    if (!account || !data) return errSecParam;
    NSMutableDictionary *query = OasisQuery(account);
    NSDictionary *attributes = @{
        (__bridge id)kSecValueData: data,
        (__bridge id)kSecAttrAccessible: (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
    };
    OSStatus status = SecItemUpdate((__bridge CFDictionaryRef)query,
                                    (__bridge CFDictionaryRef)attributes);
    if (status != errSecItemNotFound) return (int)status;
    [query addEntriesFromDictionary:attributes];
    return (int)SecItemAdd((__bridge CFDictionaryRef)query, NULL);
}

extern "C" int OasisEdgeKeychainLoad(const char *key, char **value) {
    if (!key || !value) return errSecParam;
    *value = NULL;
    NSMutableDictionary *query = OasisQuery([NSString stringWithUTF8String:key]);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
    if (status != errSecSuccess) return (int)status;
    if (!result) return errSecInternalComponent;
    NSData *data = (__bridge_transfer NSData *)result;
    NSString *text = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    if (!text) return errSecDecode;
    *value = strdup([text UTF8String]);
    return *value ? errSecSuccess : errSecAllocate;
}

extern "C" int OasisEdgeKeychainDelete(const char *key) {
    if (!key) return errSecParam;
    OSStatus status = SecItemDelete((__bridge CFDictionaryRef)OasisQuery([NSString stringWithUTF8String:key]));
    return status == errSecItemNotFound ? errSecSuccess : (int)status;
}

extern "C" void OasisEdgeKeychainFree(char *value) { if (value) free(value); }
