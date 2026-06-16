package com.krnlai.jetbrains.ui

import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.SimpleToolWindowPanel
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.jcef.JCEFHtmlPanel
import com.intellij.ui.content.ContentFactory
import com.krnlai.jetbrains.client.EditorContextProvider
import com.krnlai.jetbrains.client.KrnlAIClient
import java.awt.BorderLayout
import javax.swing.*

class ChatToolWindow : ToolWindowFactory, Disposable {

    private val logger = Logger.getInstance(ChatToolWindow::class.java)
    private var jbCefPanel: JCEFHtmlPanel? = null
    private var htmlPanel: JPanel? = null

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val client = com.krnlai.jetbrains.EarlyStartupActivity.client
        val contextProvider = EditorContextProvider(project)

        val panel = SimpleToolWindowPanel(true, true)

        val mainPanel = JPanel(BorderLayout())
        jbCefPanel = JCEFHtmlPanel(null, loadChatHtml())

        val bottomPanel = JPanel(BorderLayout())
        val inputField = JTextField()
        val sendButton = JButton("Send")

        bottomPanel.add(inputField, BorderLayout.CENTER)
        bottomPanel.add(sendButton, BorderLayout.EAST)

        mainPanel.add(jbCefPanel!!.component, BorderLayout.CENTER)
        mainPanel.add(bottomPanel, BorderLayout.SOUTH)

        sendButton.addActionListener {
            val text = inputField.text.trim()
            if (text.isNotBlank()) {
                inputField.text = ""
                val context = contextProvider.getContext(
                    com.intellij.openapi.fileEditor.FileEditorManager.getInstance(project).selectedTextEditor
                        ?: return@addActionListener
                )
                client?.sendChatMessage(
                    prompt = text,
                    context = context,
                    onChunk = { chunk ->
                        SwingUtilities.invokeLater {
                            appendToChat(jbCefPanel, chunk)
                        }
                    },
                    onComplete = { response ->
                        SwingUtilities.invokeLater {
                            val msg = if (response?.error != null) {
                                "\n<div style='color:red'>Error: ${response.error}</div>"
                            } else ""
                            appendToChat(jbCefPanel, msg)
                        }
                    }
                )
            }
        }

        inputField.addActionListener { sendButton.doClick() }

        panel.add(mainPanel)

        val content = ContentFactory.getInstance().createContent(panel, "", false)
        toolWindow.contentManager.addContent(content)
    }

    private fun loadChatHtml(): String {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Krnl-AI Chat</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1e1e1e; color: #d4d4d4; }
#messages { padding: 16px; overflow-y: auto; height: calc(100vh - 60px); }
.message { margin-bottom: 12px; padding: 8px 12px; border-radius: 8px; line-height: 1.5; }
.message.user { background: #2d2d2d; border-left: 3px solid #6366f1; }
.message.assistant { background: #252526; border-left: 3px solid #8b5cf6; }
.message .role { font-size: 11px; font-weight: 600; text-transform: uppercase; color: #888; margin-bottom: 4px; }
.message .content { white-space: pre-wrap; }
.system-message { color: #888; font-style: italic; text-align: center; padding: 20px; }
</style>
</head>
<body>
<div id="messages">
<div class="system-message">Krnl-AI Chat — Send a message to start</div>
</div>
<script>
function appendMessage(role, content) {
    var container = document.getElementById('messages');
    var div = document.createElement('div');
    div.className = 'message ' + role;
    div.innerHTML = '<div class="role">' + role + '</div><div class="content">' + content + '</div>';
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}
function appendChunk(chunk) {
    var container = document.getElementById('messages');
    var last = container.lastElementChild;
    if (last && last.className === 'message assistant') {
        var contentDiv = last.querySelector('.content');
        if (contentDiv) { contentDiv.textContent += chunk; }
    } else {
        appendMessage('assistant', chunk);
    }
    container.scrollTop = container.scrollHeight;
}
</script>
</body>
</html>
        """.trimIndent()
    }

    private fun appendToChat(panel: JCEFHtmlPanel?, text: String) {
        panel?.getCefBrowser()?.executeJavaScript(
            "appendChunk(${escapeJs(text)});", panel.component.url, 0
        )
    }

    private fun escapeJs(s: String): String {
        return s.replace("\\", "\\\\")
            .replace("'", "\\'")
            .replace("\n", "\\n")
            .replace("\r", "\\r")
    }

    override fun dispose() {
        jbCefPanel = null
        htmlPanel?.removeAll()
    }
}
