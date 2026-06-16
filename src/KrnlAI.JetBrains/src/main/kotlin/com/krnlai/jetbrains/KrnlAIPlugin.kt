package com.krnlai.jetbrains

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ProjectManagerListener
import com.intellij.openapi.startup.StartupActivity
import com.krnlai.jetbrains.client.KrnlAIClient
import com.krnlai.jetbrains.settings.KrnlAISettings

class KrnlAIPlugin : ProjectManagerListener {

    private val logger = Logger.getInstance(KrnlAIPlugin::class.java)

    override fun projectOpened(project: Project) {
        logger.info("Krnl-AI plugin initializing for project: ${project.name}")
        val settings = KrnlAISettings.getInstance()
        val client = KrnlAIClient(settings)
        EarlyStartupActivity.client = client
    }

    override fun projectClosed(project: Project) {
        logger.info("Krnl-AI plugin shutting down for project: ${project.name}")
        EarlyStartupActivity.client?.shutdown()
    }
}

class EarlyStartupActivity : StartupActivity.Background {

    companion object {
        var client: KrnlAIClient? = null
    }

    override fun runActivity(project: Project) {
        val c = client ?: return
        c.checkHealth()
    }
}
