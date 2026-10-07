package one.oasisomniverse.holooasis.unity

import android.content.ComponentName
import android.content.Context
import android.util.Base64
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch
import org.holochain.androidserviceruntime.client.HolochainServiceAdminClient
import org.holochain.androidserviceruntime.client.InstallAppPayloadFfi
import org.holochain.androidserviceruntime.client.RuntimeNetworkConfigFfi
import org.holochain.androidserviceruntime.service.HolochainService

/** JNI-stable asynchronous facade consumed by Unity's AndroidJavaProxy. */
object HolochainUnityBridge {
    interface SessionCallback {
        fun onSuccess(port: Int, authenticationTokenBase64: String)
        fun onError(errorCode: String, message: String)
    }

    interface OperationCallback {
        fun onSuccess()
        fun onError(errorCode: String, message: String)
    }

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private var client: HolochainServiceAdminClient? = null

    @JvmStatic
    fun startAndSetup(
        context: Context,
        appBundle: ByteArray,
        installedAppId: String,
        networkSeed: String?,
        bootstrapUrl: String,
        relayUrl: String,
        callback: SessionCallback,
    ) {
        if (client != null) {
            callback.onError("HOLO_ANDROID_ALREADY_STARTED", "The Holochain service client is already active.")
            return
        }
        val serviceClient = HolochainServiceAdminClient(
            context.applicationContext,
            ComponentName(context.applicationContext, HolochainService::class.java),
        )
        client = serviceClient
        try {
            serviceClient.start(RuntimeNetworkConfigFfi(bootstrapUrl, relayUrl))
        } catch (error: Throwable) {
            client = null
            callback.onError("HOLO_ANDROID_SERVICE_START_FAILED", error.message ?: error.javaClass.name)
            return
        }
        scope.launch {
            try {
                val session = serviceClient.connectSetupApp(
                    InstallAppPayloadFfi(appBundle, installedAppId, networkSeed, null, null),
                    true,
                )
                callback.onSuccess(
                    session.port.toInt(),
                    Base64.encodeToString(session.authentication.token, Base64.NO_WRAP),
                )
            } catch (error: Throwable) {
                client = null
                callback.onError("HOLO_ANDROID_APP_SETUP_FAILED", error.message ?: error.javaClass.name)
            }
        }
    }

    @JvmStatic
    fun resume(installedAppId: String, callback: SessionCallback) {
        val active = client
        if (active == null) {
            callback.onError("HOLO_ANDROID_NOT_STARTED", "The Holochain service client is not active.")
            return
        }
        scope.launch {
            try {
                val session = active.ensureAppWebsocket(installedAppId)
                callback.onSuccess(
                    session.port.toInt(),
                    Base64.encodeToString(session.authentication.token, Base64.NO_WRAP),
                )
            } catch (error: Throwable) {
                callback.onError("HOLO_ANDROID_RESUME_FAILED", error.message ?: error.javaClass.name)
            }
        }
    }

    /** The conductor remains in its foreground service while Unity is backgrounded. */
    @JvmStatic
    fun suspend(callback: OperationCallback) = callback.onSuccess()

    @JvmStatic
    fun stop(callback: OperationCallback) {
        val active = client
        if (active == null) {
            callback.onSuccess()
            return
        }
        try {
            active.stop()
            client = null
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
