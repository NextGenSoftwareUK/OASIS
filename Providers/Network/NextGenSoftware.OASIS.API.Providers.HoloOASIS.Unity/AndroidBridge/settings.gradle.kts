pluginManagement {
    repositories { google(); mavenCentral(); gradlePluginPortal() }
    resolutionStrategy {
        eachPlugin {
            if (requested.id.id == "org.jetbrains.kotlin.android") useVersion("1.6.21")
        }
    }
}
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        exclusiveContent {
            forRepository { mavenLocal() }
            filter { includeGroup("org.holochain.androidserviceruntime") }
        }
        google()
        mavenCentral()
    }
}
rootProject.name = "holooasis-unity-android-bridge"
include(":bridge")
