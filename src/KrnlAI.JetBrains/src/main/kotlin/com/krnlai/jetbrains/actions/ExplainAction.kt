package com.krnlai.jetbrains.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.DialogBuilder
import com.intellij.openapi.ui.Messages
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.EditorContextProvider
import javax.swing.*

class ExplainAction : AnAction() {

    private val logger = Logger.getInstance(ExplainAction::class.java)

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val client = EarlyStartupActivity.client ?: return

        val contextProvider = EditorContextProvider(project)
        val selectedText = editor.caretModel.currentCaret.selectedText

        if (selectedText.isNullOrBlank()) {
            Messages.showWarningDialog(
                project,
                "No code selected. Select code in the editor and try again.",
                "/explain — No Selection"
            )
            return
        }

        val prompt = "/explain $selectedText"
        client.sendChatMessage(
            prompt = prompt,
            context = contextProvider.getContext(editor),
            onComplete = { response ->
                val result = response?.narration ?: "No response from Krnl-AI."
                SwingUtilities.invokeLater {
                    val builder = DialogBuilder(project)
                    builder.title = "/explain"
                    builder.setNorthLabel("Explanation:")
                    val textArea = JTextArea(result)
                    textArea.isEditable = false
                    textArea.lineWrap = true
                    textArea.wrapStyleWord = true
                    textArea.rows = 15
                    textArea.columns = 60
                    builder.centerPanel = JScrollPane(textArea)
                    builder.addCloseButton()
                    builder.show()
                }
            }
        )
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isEnabledAndVisible = project != null && editor != null
    }
}
