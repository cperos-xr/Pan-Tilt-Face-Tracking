// settings.gradle.kts

pluginManagement {
  repositories {
    gradlePluginPortal()
    google()
    mavenCentral()
    maven("https://jitpack.io")
  }
  plugins {
    id("com.android.application") version "8.0.2"
    id("com.android.library")     version "8.0.2"
    kotlin("android")             version "1.8.10"
  }
}

dependencyResolutionManagement {
  repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
  repositories {
    google()
    mavenCentral()
    maven("https://jitpack.io")
  }
}

rootProject.name = "SerialManager"
include(":app", ":SerialPlugin")
