package one.oasisomniverse.holooasis.unity

import android.util.Base64
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import org.java_websocket.client.WebSocketClient
import org.java_websocket.handshake.ServerHandshake
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.msgpack.core.MessagePack
import org.msgpack.value.Value
import java.net.URI
import java.nio.ByteBuffer
import java.security.SecureRandom
import java.util.concurrent.CountDownLatch
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.TimeUnit

/**
 * Runs the real bridge and Holochain conductor on a device/emulator:
 * start (prefer shared → falls back to embedded when it is absent, incompatible or unapproved) → authenticate →
 * app_info → sign a zome call through the bridge (conductor-held key) → call_zome → zome_called.
 */
@RunWith(AndroidJUnit4::class)
class HolochainUnityBridgeDeviceTest {
    private val context = InstrumentationRegistry.getInstrumentation().targetContext
    private val testContext = InstrumentationRegistry.getInstrumentation().context

    @After
    fun tearDown() {
        val done = CountDownLatch(1)
        HolochainUnityBridge.stop(object : HolochainUnityBridge.OperationCallback {
            override fun onSuccess() = done.countDown()
            override fun onError(errorCode: String, message: String) = done.countDown()
        })
        done.await(30, TimeUnit.SECONDS)
    }

    @Test
    fun fallsBackToEmbedded_signsThroughConductor_andZomeCallSucceeds() {
        val bundle = testContext.assets.open("oasis.happ").use { it.readBytes() }
        val appId = "oasis-device-test-" + System.currentTimeMillis()
        val statuses = mutableListOf<String>()

        // 1. Start, preferring the shared runtime (not installed here → embedded fallback).
        val started = CountDownLatch(1)
        var port = 0
        var token = ""
        var startError: String? = null
        HolochainUnityBridge.startAndSetup(
            context, bundle, appId, null,
            "https://dev-test-bootstrap2.holochain.org", "https://use1-1.relay.n0.iroh-canary.iroh.link./",
            true, 5_000L,
            object : HolochainUnityBridge.StatusCallback {
                override fun onStatus(status: String, message: String) { statuses.add(status) }
            },
            object : HolochainUnityBridge.SessionCallback {
                override fun onSuccess(port: Int, authenticationTokenBase64: String) {
                    portHolder[0] = port; tokenHolder[0] = authenticationTokenBase64; started.countDown()
                }
                override fun onError(errorCode: String, message: String) {
                    errorHolder[0] = "$errorCode: $message"; started.countDown()
                }
            },
        )
        assertTrue("conductor did not start in time", started.await(420, TimeUnit.SECONDS))
        startError = errorHolder[0]
        assertEquals(null, startError)
        port = portHolder[0]; token = tokenHolder[0]
        assertTrue(port in 1..65535)
        assertTrue(token.isNotEmpty())
        android.util.Log.i("HoloBridgeTest", "mode=${HolochainUnityBridge.mode} statuses=$statuses")
        if (HolochainUnityBridge.isSharedRuntimeInstalled(context) &&
            HolochainUnityBridge.mode == HolochainUnityBridge.MODE_EMBEDDED) {
            assertTrue("shared failure must be reported before recovery",
                statuses.indexOf("shared_unavailable") >= 0 &&
                    statuses.indexOf("fallback") > statuses.indexOf("shared_unavailable"))
        } else if (HolochainUnityBridge.isSharedRuntimeInstalled(context)) {
            assertEquals(HolochainUnityBridge.MODE_SHARED, HolochainUnityBridge.mode)
        } else {
            assertEquals(HolochainUnityBridge.MODE_EMBEDDED, HolochainUnityBridge.mode)
        }

        // 2. Connect to the app interface and authenticate (token is a Vec<u8> → msgpack int array).
        val ws = AppSocket(port)
        ws.authenticate(Base64.decode(token, Base64.NO_WRAP))

        // 3. app_info → agent key and cell id.
        val appInfo = ws.request("app_info", null)
        assertEquals("app_info", appInfo.type)
        val info = appInfo.value!!.asMapValue().map()
        val agent = info[str("agent_pub_key")]!!.asBinaryValue().asByteArray()
        val cell = info[str("cell_info")]!!.asMapValue().map().values.first().asArrayValue().get(0)
            .asMapValue().map()[str("value")]!!.asMapValue().map()[str("cell_id")]!!.asArrayValue()
        val dna = cell.get(0).asBinaryValue().asByteArray()
        assertEquals(39, agent.size)
        assertEquals(39, dna.size)

        // 4. Sign get_all_avatars(()) through the bridge: the conductor holds the agent key.
        val nonce = ByteArray(32).also { SecureRandom().nextBytes(it) }
        val unitPayload = byteArrayOf(0xc0.toByte()) // msgpack nil == Rust ()
        val signed = CountDownLatch(1)
        HolochainUnityBridge.signZomeCall(
            b64(agent), b64(dna), b64(agent), "oasis", "get_all_avatars", null,
            b64(unitPayload), b64(nonce), (System.currentTimeMillis() + 5 * 60 * 1000) * 1000,
            object : HolochainUnityBridge.SignCallback {
                override fun onSuccess(bytesBase64: String, signatureBase64: String) {
                    signedHolder[0] = bytesBase64; signedHolder[1] = signatureBase64; signed.countDown()
                }
                override fun onError(errorCode: String, message: String) {
                    errorHolder[0] = "$errorCode: $message"; signed.countDown()
                }
            },
        )
        assertTrue(signed.await(60, TimeUnit.SECONDS))
        assertEquals(null, errorHolder[0])
        val signature = Base64.decode(signedHolder[1], Base64.NO_WRAP)
        assertEquals(64, signature.size)

        // 5. Send the signed call; the conductor must accept its own signature.
        val result = ws.request("call_zome", mapOf(
            "bytes" to Base64.decode(signedHolder[0], Base64.NO_WRAP),
            "signature" to signature,
        ))
        assertEquals("zome_called (got ${result.value})", "zome_called", result.type)
        ws.close()
    }

    // --- tiny msgpack/websocket helpers -------------------------------------------------------

    private val portHolder = IntArray(1)
    private val tokenHolder = arrayOf("")
    private val errorHolder = arrayOfNulls<String>(1)
    private val signedHolder = arrayOf("", "")

    private fun b64(bytes: ByteArray) = Base64.encodeToString(bytes, Base64.NO_WRAP)
    private fun str(s: String): Value = org.msgpack.value.ValueFactory.newString(s)

    private class Envelope(val type: String, val value: Value?)

    private class AppSocket(port: Int) {
        private val inbox = LinkedBlockingQueue<ByteArray>()
        private var nextId = 1L
        private val client = object : WebSocketClient(URI("ws://127.0.0.1:$port")) {
            override fun onOpen(handshakedata: ServerHandshake?) {}
            override fun onMessage(message: String?) {}
            override fun onMessage(bytes: ByteBuffer) {
                val copy = ByteArray(bytes.remaining()); bytes.get(copy); inbox.add(copy)
            }
            override fun onClose(code: Int, reason: String?, remote: Boolean) {}
            override fun onError(ex: Exception?) {}
        }

        init {
            client.addHeader("Origin", "http://localhost")
            check(client.connectBlocking(30, TimeUnit.SECONDS)) { "app websocket did not connect" }
        }

        fun authenticate(token: ByteArray) {
            val inner = pack { p ->
                p.packMapHeader(1); p.packString("token"); p.packArrayHeader(token.size)
                token.forEach { p.packInt(it.toInt() and 0xff) }
            }
            client.send(pack { p ->
                p.packMapHeader(2); p.packString("type"); p.packString("authenticate")
                p.packString("data"); p.packBinaryHeader(inner.size); p.writePayload(inner)
            })
            Thread.sleep(250)
            check(client.isOpen) { "conductor closed the socket after authenticate" }
        }

        fun request(type: String, value: Map<String, ByteArray>?): Envelope {
            val inner = pack { p ->
                p.packMapHeader(2); p.packString("type"); p.packString(type); p.packString("value")
                if (value == null) p.packNil() else {
                    p.packMapHeader(value.size)
                    value.forEach { (k, v) -> p.packString(k); p.packBinaryHeader(v.size); p.writePayload(v) }
                }
            }
            val id = nextId++
            client.send(pack { p ->
                p.packMapHeader(3); p.packString("type"); p.packString("request")
                p.packString("id"); p.packLong(id)
                p.packString("data"); p.packBinaryHeader(inner.size); p.writePayload(inner)
            })
            val frame = inbox.poll(60, TimeUnit.SECONDS) ?: error("no response to $type")
            val outer = MessagePack.newDefaultUnpacker(frame).unpackValue().asMapValue().map()
            val data = outer[ValueFactory("data")]!!.asBinaryValue().asByteArray()
            val env = MessagePack.newDefaultUnpacker(data).unpackValue().asMapValue().map()
            return Envelope(env[ValueFactory("type")]!!.asStringValue().asString(), env[ValueFactory("value")])
        }

        fun close() = client.closeBlocking()

        private fun ValueFactory(s: String): Value = org.msgpack.value.ValueFactory.newString(s)

        private fun pack(block: (org.msgpack.core.MessagePacker) -> Unit): ByteArray {
            val packer = MessagePack.newDefaultBufferPacker()
            block(packer)
            packer.close()
            return packer.toByteArray()
        }
    }
}
