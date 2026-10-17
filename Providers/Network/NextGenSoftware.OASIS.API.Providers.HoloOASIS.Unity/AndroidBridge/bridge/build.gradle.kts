plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "one.oasisomniverse.holooasis.unity"
    compileSdk = 34
    defaultConfig {
        // The conductor FFI ships for arm64 only; keep the process arm64 so it can load (x86_64 emulators run it via translation).
        ndk { abiFilters += "arm64-v8a" }
        minSdk = 27
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
    kotlinOptions { jvmTarget = "1.8" }
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib:1.6.21")
    implementation("org.holochain.androidserviceruntime:client:0.0.19")
    implementation("org.holochain.androidserviceruntime:service:0.0.19")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.6.4")

    // On-device tests only (not packaged into the Unity plugin).
    androidTestImplementation("androidx.test:runner:1.5.2")
    androidTestImplementation("androidx.test.ext:junit:1.1.5")
    androidTestImplementation("junit:junit:4.13.2")
    androidTestImplementation("org.msgpack:msgpack-core:0.9.8")
    androidTestImplementation("org.java-websocket:Java-WebSocket:1.5.7")
}

configurations.configureEach {
    resolutionStrategy.force(
        "org.jetbrains.kotlin:kotlin-stdlib:1.6.21",
        "org.jetbrains.kotlin:kotlin-stdlib-common:1.6.21",
        "org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.6.21",
        "org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.6.21",
    )
}

// Unity does not consume Maven POM metadata from loose AAR plugins. Export the exact
// resolved runtime closure so the HoloEnabled UPM profile is self-contained and does
// not accidentally select the stale public 0.0.19 Holochain runtime at player-build time.
tasks.register<Copy>("exportReleaseRuntimeDependencies") {
    from(configurations.named("releaseRuntimeClasspath"))
    into(layout.buildDirectory.dir("unityRuntimeDependencies"))
    exclude("client-0.0.19.aar", "service-0.0.19.aar")
}
