package com.krnlai.jetbrains.ui

import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.SimpleToolWindowPanel
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.jcef.JCEFHtmlPanel
import com.intellij.ui.content.ContentFactory
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.CognitiveDashboardResponse
import com.krnlai.jetbrains.client.EmotionalStateResponse
import kotlinx.coroutines.*
import javax.swing.*

class DashboardToolWindow : ToolWindowFactory, Disposable {

    private val logger = Logger.getInstance(DashboardToolWindow::class.java)
    private var jbCefPanel: JCEFHtmlPanel? = null
    private var refreshJob: Job? = null
    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val client = EarlyStartupActivity.client
        val panel = SimpleToolWindowPanel(true, true)

        jbCefPanel = JCEFHtmlPanel(null, loadDashboardHtml())
        panel.add(jbCefPanel!!.component)

        val content = ContentFactory.getInstance().createContent(panel, "", false)
        toolWindow.contentManager.addContent(content)

        refreshJob = scope.launch {
            while (isActive) {
                val dashboard = client?.getDashboard()
                val emotional = client?.getEmotionalState()
                SwingUtilities.invokeLater {
                    updateDashboard(jbCefPanel, dashboard, emotional)
                }
                delay(5000)
            }
        }
    }

    private fun loadDashboardHtml(): String {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Krnl-AI Dashboard</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1e1e1e; color: #d4d4d4; padding: 16px; }
.card { background: #252526; border-radius: 8px; padding: 16px; margin-bottom: 12px; }
.card h3 { font-size: 12px; text-transform: uppercase; color: #888; margin-bottom: 8px; }
.metric { display: flex; justify-content: space-between; padding: 4px 0; font-size: 13px; }
.metric .value { font-weight: 600; color: #8b5cf6; }
.health-good { color: #4ade80; }
.health-warn { color: #facc15; }
.health-bad { color: #f87171; }
.emotion { font-size: 32px; text-align: center; padding: 12px; }
.module { display: inline-block; background: #2d2d2d; padding: 4px 8px; border-radius: 4px; margin: 2px; font-size: 12px; }
</style>
</head>
<body>
<div id="content">
<div class="card">
<h3>System Health</h3>
<div class="metric"><span>Overall</span><span id="overallHealth" class="health-good">--</span></div>
</div>
<div class="card">
<h3>Emotional State</h3>
<div id="emotionDisplay" class="emotion">--</div>
</div>
<div class="card">
<h3>Active Modules</h3>
<div id="activeModules">--</div>
</div>
<div class="card">
<h3>Recent Events</h3>
<div id="recentEvents">--</div>
</div>
</div>
<script>
function updateDashboard(data) {
    if (!data) return;
    var health = data.overallHealth;
    var healthEl = document.getElementById('overallHealth');
    if (health != null) {
        var pct = (health * 100).toFixed(0);
        healthEl.textContent = pct + '%';
        healthEl.className = health > 0.7 ? 'health-good' : health > 0.3 ? 'health-warn' : 'health-bad';
    }
    var modules = data.activeModules;
    var modEl = document.getElementById('activeModules');
    if (modules && modules.length > 0) {
        modEl.innerHTML = modules.map(function(m) { return '<span class="module">' + m + '</span>'; }).join('');
    }
}
function updateEmotion(emotion) {
    if (!emotion) return;
    var el = document.getElementById('emotionDisplay');
    var valence = emotion.valence || 0;
    var arousal = emotion.arousal || 0;
    var emoji = valence > 0.3 ? (arousal > 0.3 ? '😊' : '😌') : valence < -0.3 ? (arousal > 0.3 ? '😠' : '😢') : '😐';
    el.textContent = emoji + ' (' + (valence * 100).toFixed(0) + '%, ' + (arousal * 100).toFixed(0) + '%)';
}
</script>
</body>
</html>
        """.trimIndent()
    }

    private fun updateDashboard(
        panel: JCEFHtmlPanel?,
        dashboard: CognitiveDashboardResponse?,
        emotional: EmotionalStateResponse?
    ) {
        val browser = panel?.cefBrowser ?: return
        if (dashboard != null) {
            val json = """
                { overallHealth: ${dashboard.overallHealth ?: 0.0},
                  activeModules: ${gson.toJson(dashboard.activeModules ?: emptyList<Any>())},
                  recentEvents: ${gson.toJson(dashboard.recentEvents ?: emptyList<Any>())} }
            """.trimIndent()
            browser.executeJavaScript("updateDashboard($json);", panel.component.url, 0)
        }
        if (emotional != null) {
            val json = "{ valence: ${emotional.valence ?: 0.0}, arousal: ${emotional.arousal ?: 0.0} }"
            browser.executeJavaScript("updateEmotion($json);", panel.component.url, 0)
        }
    }

    override fun dispose() {
        refreshJob?.cancel()
        jbCefPanel = null
    }

    companion object {
        private val gson = com.google.gson.Gson()
    }
}
