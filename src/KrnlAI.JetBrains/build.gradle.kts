plugins {
    id("org.jetbrains.intellij") version "2.2.1"
    kotlin("jvm") version "2.1.0"
}

group = "com.krnlai"
version = providers.gradleProperty("pluginVersion").get()

repositories {
    mavenCentral()
}

intellij {
    version.set("2024.3")
    type.set("IC")
    plugins.set(listOf("com.intellij.java", "com.intellij.jcef"))
}

dependencies {
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.google.code.gson:gson:2.11.0")
}

tasks {
    patchPluginXml {
        sinceBuild.set(providers.gradleProperty("pluginSinceBuild"))
        untilBuild.set(providers.gradleProperty("pluginUntilBuild"))
    }

    buildSearchableOptions {
        enabled = false
    }
}
