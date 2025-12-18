// SerialPlugin/build.gradle.kts
plugins {
    id("com.android.library")
    kotlin("android")                // if you want Kotlin support; omit if pure Java
}

android {
    namespace = "com.yourname.serialplugin"
    compileSdk = 33

    defaultConfig {
        minSdk = 21                 // OTG/USB Host support from API 12+, but choose 21+ for broad device coverage
        targetSdk = 33
    }

    // Optional: if you need Java 8+ APIs
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
    kotlinOptions {
        jvmTarget = "1.8"
    }

    // If you need to generate AAR with sources/javadoc:
    publishing {
        singleVariant("release") {
            withSourcesJar()
            withJavadocJar()
        }
    }
}

dependencies {
    // USB-serial driver supporting CDC, FTDI, CH34x, Prolific, etc.
    implementation("com.github.mik3y:usb-serial-for-android:3.9.0")
    
    // If you use androidx libraries, add them here:
    // implementation("androidx.core:core-ktx:1.9.0")
}
