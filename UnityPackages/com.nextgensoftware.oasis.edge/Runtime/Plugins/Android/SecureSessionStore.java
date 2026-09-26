package one.oasisomniverse.edge;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import com.unity3d.player.UnityPlayer;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.MessageDigest;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class SecureSessionStore {
    private static final String STORE = "oasis_edge_secure_sessions";
    private static final String KEYSTORE = "AndroidKeyStore";

    public static void save(String key, String base64Value) throws Exception {
        SecretKey secretKey = getOrCreateKey(alias(key));
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.ENCRYPT_MODE, secretKey);
        byte[] encrypted = cipher.doFinal(base64Value.getBytes(StandardCharsets.UTF_8));
        byte[] payload = new byte[cipher.getIV().length + encrypted.length + 1];
        payload[0] = (byte)cipher.getIV().length;
        System.arraycopy(cipher.getIV(), 0, payload, 1, cipher.getIV().length);
        System.arraycopy(encrypted, 0, payload, cipher.getIV().length + 1, encrypted.length);
        preferences().edit().putString(key, Base64.encodeToString(payload, Base64.NO_WRAP)).commit();
    }

    public static String load(String key) throws Exception {
        String stored = preferences().getString(key, null);
        if (stored == null) return null;
        byte[] payload = Base64.decode(stored, Base64.NO_WRAP);
        int ivLength = payload[0] & 0xff;
        if (ivLength < 12 || payload.length <= ivLength + 1) throw new IllegalStateException("Invalid protected session payload.");
        byte[] iv = new byte[ivLength];
        byte[] encrypted = new byte[payload.length - ivLength - 1];
        System.arraycopy(payload, 1, iv, 0, ivLength);
        System.arraycopy(payload, ivLength + 1, encrypted, 0, encrypted.length);
        KeyStore store = KeyStore.getInstance(KEYSTORE);
        store.load(null);
        SecretKey secretKey = (SecretKey)store.getKey(alias(key), null);
        if (secretKey == null) throw new IllegalStateException("The device-bound encryption key is missing.");
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, secretKey, new GCMParameterSpec(128, iv));
        return new String(cipher.doFinal(encrypted), StandardCharsets.UTF_8);
    }

    public static void delete(String key) {
        preferences().edit().remove(key).commit();
    }

    private static SecretKey getOrCreateKey(String alias) throws Exception {
        KeyStore store = KeyStore.getInstance(KEYSTORE);
        store.load(null);
        SecretKey existing = (SecretKey)store.getKey(alias, null);
        if (existing != null) return existing;
        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, KEYSTORE);
        generator.init(new KeyGenParameterSpec.Builder(alias,
            KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .build());
        return generator.generateKey();
    }

    private static String alias(String key) throws Exception {
        byte[] digest = MessageDigest.getInstance("SHA-256").digest(key.getBytes(StandardCharsets.UTF_8));
        return "oasis-edge-" + Base64.encodeToString(digest, Base64.NO_WRAP | Base64.URL_SAFE);
    }

    private static SharedPreferences preferences() {
        return UnityPlayer.currentActivity.getSharedPreferences(STORE, Context.MODE_PRIVATE);
    }
}
