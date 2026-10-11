package one.oasisomniverse.holooasis.unity

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.ComponentName
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.util.Base64
import android.util.Log
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.TimeoutCancellationException
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeout
import org.holochain.androidserviceruntime.client.AppAuthFfi
import org.holochain.androidserviceruntime.client.AppBinderUnauthorizedException
import org.holochain.androidserviceruntime.client.CellIdFfi
import org.holochain.androidserviceruntime.client.HolochainServiceAdminClient
import org.holochain.androidserviceruntime.client.HolochainServiceAppClient
import org.holochain.androidserviceruntime.client.InstallAppPayloadFfi
import org.holochain.androidserviceruntime.client.RuntimeNetworkConfigFfi
import org.holochain.androidserviceruntime.client.ZomeCallParamsFfi
import org.holochain.androidserviceruntime.service.HolochainService

/**
 * JNI-stable asynchronous facade consumed by Unity's AndroidJavaProxy.
 *
 * Two modes:
 * - shared: the phone already has the Holochain Service Runtime app installed. We use its conductor
 *   (one conductor for every hApp on the phone) through the restricted app binder. The player approves
 *   access once via a runtime notification.
 * - embedded: our own HolochainService runs inside this app (the original behaviour, and the fallback
 *   when the shared runtime is missing, incompatible or not approved in time).
 *
 * Zome calls are signed by the conductor (signZomeCall) in both modes; HoloNET has no admin access here
 * to grant itself signing credentials.
 */
object HolochainUnityBridge {
    const val SHARED_RUNTIME_PACKAGE = "org.holochain.androidserviceruntime.app"
    const val MODE_EMBEDDED = "embedded"
    const val MODE_SHARED = "shared"

    interface SessionCallback {
        fun onSuccess(port: Int, authenticationTokenBase64: String)
        fun onError(errorCode: String, message: String)
    }

    interface OperationCallback {
        fun onSuccess()
        fun onError(errorCode: String, message: String)
    }

    interface SignCallback {
        fun onSuccess(bytesBase64: String, signatureBase64: String)
        fun onError(errorCode: String, message: String)
    }

    /** Optional progress notifications, e.g. to show "approve Holochain access" in game UI. */
    interface StatusCallback {
        fun onStatus(status: String, message: String)
    }

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private var adminClient: HolochainServiceAdminClient? = null
    private var appClient: HolochainServiceAppClient? = null
    private var setupPayload: InstallAppPayloadFfi? = null

    @JvmStatic
    var mode: String? = null
        private set

    /** True when the shared Holochain Service Runtime app is installed on this device. */
    @JvmStatic
    fun isSharedRuntimeInstalled(context: Context): Boolean =
        try {
            context.packageManager.getPackageInfo(SHARED_RUNTIME_PACKAGE, 0)
            true
        } catch (_: PackageManager.NameNotFoundException) {
            false
        }

    /** Original entry point: always uses the embedded conductor. */
    @JvmStatic
    fun startAndSetup(
        context: Context,
        appBundle: ByteArray,
        installedAppId: String,
        networkSeed: String?,
        bootstrapUrl: String,
        relayUrl: String,
        callback: SessionCallback,
    ) = startAndSetup(context, appBundle, installedAppId, networkSeed, bootstrapUrl, relayUrl, false, 0L, null, callback)

    /**
     * Prefers the shared runtime when [preferShared] is set and it is installed, waiting up to
     * [approvalTimeoutMs] for the player to approve access. Shared setup failures are logged and
     * reported through [status], then automatically recover using the built-in conductor.
     */
    @JvmStatic
    fun startAndSetup(
        context: Context,
        appBundle: ByteArray,
        installedAppId: String,
        networkSeed: String?,
        bootstrapUrl: String,
        relayUrl: String,
        preferShared: Boolean,
        approvalTimeoutMs: Long,
        status: StatusCallback?,
        callback: SessionCallback,
    ) {
        if (adminClient != null || appClient != null) {
            callback.onError("HOLO_ANDROID_ALREADY_STARTED", "The Holochain service client is already active.")
            return
        }
        val payload = InstallAppPayloadFfi(appBundle, installedAppId, networkSeed, null, null)
        setupPayload = payload

        scope.launch {
            if (preferShared && isSharedRuntimeInstalled(context)) {
                try {
                    val shared = trySharedSetup(context, payload, approvalTimeoutMs, status)
                    mode = MODE_SHARED
                    status?.onStatus("shared", "Using the shared Holochain runtime.")
                    callback.onSuccess(shared.port.toInt(), Base64.encodeToString(shared.authentication.token, Base64.NO_WRAP))
                } catch (error: Exception) {
                    if (error is CancellationException && error !is TimeoutCancellationException) throw error
                    mode = null
                    val message = "HOLO_ANDROID_SHARED_SETUP_FAILED: ${error.message ?: error.javaClass.name}"
                    Log.w("HolochainUnityBridge", message, error)
                    status?.onStatus("shared_unavailable", message)
                    status?.onStatus("fallback", "Switching automatically to the built-in Holochain conductor.")
                    startEmbedded(context, payload, bootstrapUrl, relayUrl, callback)
                }
                return@launch
            }
            startEmbedded(context, payload, bootstrapUrl, relayUrl, callback)
        }
    }

    private suspend fun trySharedSetup(
        context: Context,
        payload: InstallAppPayloadFfi,
        approvalTimeoutMs: Long,
        status: StatusCallback?,
    ): AppAuthFfi {
        val client = HolochainServiceAppClient(
            context,
            ComponentName(SHARED_RUNTIME_PACKAGE, HolochainService::class.java.name),
        )
        return try {
            withTimeout(approvalTimeoutMs.coerceAtLeast(1L)) {
                var notifiedAwaiting = false
                while (true) {
                    try {
                        val auth = client.connectSetupApp(payload, true)
                        appClient = client
                        return@withTimeout auth
                    } catch (_: AppBinderUnauthorizedException) {
                        // The runtime has posted an approval notification; wait for the player.
                        if (!notifiedAwaiting) {
                            status?.onStatus("awaiting_approval", "Approve Holochain access in the notification to use the shared runtime.")
                            notifiedAwaiting = true
                        }
                        delay(2000)
                    }
                }
                @Suppress("UNREACHABLE_CODE")
                error("Shared runtime setup did not return an authenticated session.")
            }
        } catch (error: Throwable) {
            // The caller reports the failure and performs the specified embedded recovery.
            throw error
        }
    }

    // HolochainService posts its foreground notification on this channel but expects the host app to create it.
    private const val FOREGROUND_CHANNEL_ID = "org.holochain.androidserviceruntime.service.FOREGROUND_SERVICE"

    private fun ensureForegroundChannel(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val manager = context.getSystemService(NotificationManager::class.java) ?: return
        if (manager.getNotificationChannel(FOREGROUND_CHANNEL_ID) == null) {
            manager.createNotificationChannel(
                NotificationChannel(FOREGROUND_CHANNEL_ID, "Holochain", NotificationManager.IMPORTANCE_LOW),
            )
        }
    }

    private suspend fun startEmbedded(
        context: Context,
        payload: InstallAppPayloadFfi,
        bootstrapUrl: String,
        relayUrl: String,
        callback: SessionCallback,
    ) {
        val serviceClient = HolochainServiceAdminClient(
            context.applicationContext,
            ComponentName(context.applicationContext, HolochainService::class.java),
        )
        adminClient = serviceClient
        try {
            ensureForegroundChannel(context.applicationContext)
            serviceClient.start(RuntimeNetworkConfigFfi(bootstrapUrl, relayUrl))
        } catch (error: Throwable) {
            adminClient = null
            callback.onError("HOLO_ANDROID_SERVICE_START_FAILED", error.message ?: error.javaClass.name)
            return
        }
        try {
            // start() returns before the conductor is up, and isReady() needs a bound service; the
            // service rejects calls until the conductor is ready (slow on first start / emulators).
            serviceClient.connect()
            withTimeout(300_000L) { while (!serviceClient.isReady()) delay(500) }
            val session = serviceClient.setupApp(payload, true)
            mode = MODE_EMBEDDED
            callback.onSuccess(session.port.toInt(), Base64.encodeToString(session.authentication.token, Base64.NO_WRAP))
        } catch (error: Throwable) {
            adminClient = null
            callback.onError("HOLO_ANDROID_APP_SETUP_FAILED", error.message ?: error.javaClass.name)
        }
    }

    @JvmStatic
    fun resume(installedAppId: String, callback: SessionCallback) {
        val admin = adminClient
        val app = appClient
        val payload = setupPayload
        if (admin == null && app == null) {
            callback.onError("HOLO_ANDROID_NOT_STARTED", "The Holochain service client is not active.")
            return
        }
        scope.launch {
            try {
                // The app client has no ensureAppWebsocket wrapper; setupApp is idempotent for an
                // installed app and returns a fresh port and token.
                val session = admin?.ensureAppWebsocket(installedAppId) ?: app!!.connectSetupApp(payload!!, true)
                callback.onSuccess(session.port.toInt(), Base64.encodeToString(session.authentication.token, Base64.NO_WRAP))
            } catch (error: Throwable) {
                callback.onError("HOLO_ANDROID_RESUME_FAILED", error.message ?: error.javaClass.name)
            }
        }
    }

    /**
     * Signs a zome call with the agent key held by the conductor. Byte arrays are base64 for JNI.
     * [capSecretBase64] may be empty: the agent's own calls need no cap secret.
     */
    @JvmStatic
    fun signZomeCall(
        provenanceBase64: String,
        dnaHashBase64: String,
        agentPubKeyBase64: String,
        zomeName: String,
        fnName: String,
        capSecretBase64: String?,
        payloadBase64: String,
        nonceBase64: String,
        expiresAt: Long,
        callback: SignCallback,
    ) {
        val admin = adminClient
        val app = appClient
        if (admin == null && app == null) {
            callback.onError("HOLO_ANDROID_NOT_STARTED", "The Holochain service client is not active.")
            return
        }
        val args = ZomeCallParamsFfi(
            decode(provenanceBase64),
            CellIdFfi(decode(dnaHashBase64), decode(agentPubKeyBase64)),
            zomeName,
            fnName,
            if (capSecretBase64.isNullOrEmpty()) null else decode(capSecretBase64),
            decode(payloadBase64),
            decode(nonceBase64),
            expiresAt,
        )
        scope.launch {
            try {
                val signed = admin?.signZomeCall(args) ?: app!!.signZomeCall(args)
                callback.onSuccess(
                    Base64.encodeToString(signed.bytes, Base64.NO_WRAP),
                    Base64.encodeToString(signed.signature, Base64.NO_WRAP),
                )
            } catch (error: Throwable) {
                callback.onError("HOLO_ANDROID_SIGN_FAILED", error.message ?: error.javaClass.name)
            }
        }
    }

    private fun decode(value: String): ByteArray = Base64.decode(value, Base64.NO_WRAP)

    /** The conductor remains in its foreground service while Unity is backgrounded. */
    @JvmStatic
    fun suspend(callback: OperationCallback) = callback.onSuccess()

    /** Stops the embedded conductor. In shared mode the runtime belongs to the phone, so it keeps running. */
    @JvmStatic
    fun stop(callback: OperationCallback) {
        val admin = adminClient
        appClient = null
        if (admin == null) {
            mode = null
            callback.onSuccess()
            return
        }
        try {
            admin.stop()
            adminClient = null
            mode = null
            callback.onSuccess()
        } catch (error: Throwable) {
            callback.onError("HOLO_ANDROID_SERVICE_STOP_FAILED", error.message ?: error.javaClass.name)
        }
    }

    @JvmStatic
    fun dispose(callback: OperationCallback) {
        stop(object : OperationCallback {
            override fun onSuccess() {
                scope.cancel()
                callback.onSuccess()
            }

            override fun onError(errorCode: String, message: String) = callback.onError(errorCode, message)
        })
    }
}
