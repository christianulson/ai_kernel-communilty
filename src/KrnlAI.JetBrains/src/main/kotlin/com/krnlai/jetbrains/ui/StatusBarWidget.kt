package com.krnlai.jetbrains.ui

import com.intellij.icons.AllIcons
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.impl.status.EditorBasedWidget
import com.intellij.util.Consumer
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.EmotionalStateResponse
import kotlinx.coroutines.*
import java.awt.Component
import java.awt.event.MouseEvent
import javax.swing.*

class StatusBarWidget(project: Project) : EditorBasedWidget(project), StatusBarWidget.Multiframe {

    private val logger = Logger.getInstance(StatusBarWidget::class.java)
    private var currentEmotion: EmotionalStateResponse? = null
    private var isConnected = false
    private var refreshJob: Job? = null
    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    private val label = JLabel()

    init {
        label.text = " Krnl-AI: \u23F3"
        label.toolTipText = "Krnl-AI — Connecting..."
        updatePresentation()
        startPolling()
    }

    private fun startPolling() {
        refreshJob?.cancel()
        refreshJob = scope.launch {
            while (isActive) {
                try {
                    val client = EarlyStartupActivity.client
                    if (client != null) {
                        val health = client.checkHealth()
                        isConnected = health != null
                        currentEmotion = client.getEmotionalState()
                    } else {
                        isConnected = false
                        currentEmotion = null
                    }
                } catch (e: Exception) {
                    isConnected = false
                    currentEmotion = null
                }
                SwingUtilities.invokeLater { updatePresentation() }
                delay(5000)
            }
        }
    }

    private fun updatePresentation() {
        val emotion = currentEmotion?.let { e ->
            val v = e.valence ?: 0.0
            val a = e.arousal ?: 0.0
            when {
                v > 0.3 && a > 0.3 -> "\uD83D\uDE0A"
                v > 0.3 -> "\uD83D\uDE0C"
                v < -0.3 && a > 0.3 -> "\uD83D\uDE20"
                v < -0.3 -> "\uD83D\uDE22"
                else -> "\uD83D\uDE10"
            }
        }
        val mood = emotion ?: "\uD83E\uDD14"
        val status = if (isConnected) "\u26A1" else "\u26D4"
        label.text = " $mood $status Krnl-AI"
        label.toolTipText = when {
            isConnected -> "Krnl-AI connected | Mood: ${currentEmotion?.let { "valence=${it.valence}, arousal=${it.arousal}" } ?: "unknown"}"
            else -> "Krnl-AI disconnected — Check Sidecar on port 5001"
        }
    }

    override fun ID() = "KrnlAI.StatusBar"

    override fun getPresentation() = object : StatusBarWidget.TextPresentation {
        override fun getText() = label.text
        override fun getAlignment() = Component.LEFT_ALIGNMENT
        override fun getIcon() = null
        override fun getTooltipText() = label.toolTipText
    }

    override fun getClickConsumer() = Consumer<MouseEvent> {
        com.intellij.openapi.wm.ToolWindowManager.getInstance(project)
            .getToolWindow("KrnlAI.Dashboard")
            ?.show()
    }

    override fun dispose() {
        refreshJob?.cancel()
        scope.cancel()
        super.dispose()
    }

    override fun getWidget() = this
}
