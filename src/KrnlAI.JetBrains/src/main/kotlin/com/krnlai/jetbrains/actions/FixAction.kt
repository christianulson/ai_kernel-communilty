package com.krnlai.jetbrains.actions

import com.intellij.codeInsight.daemon.DaemonCodeAnalyzer
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.DialogBuilder
import com.intellij.openapi.ui.Messages
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.EditorContextProvider
import javax.swing.*

class FixAction : AnAction() {

    private val logger = Logger.getInstance(FixAction::class.java)

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val client = EarlyStartupActivity.client ?: return

        val contextProvider = EditorContextProvider(project)
        val context = contextProvider.getContext(editor) ?: return

        val diagCount = context.diagnostics?.size ?: 0
        val diagnosticsText = context.diagnostics?.joinToString("\n") { d ->
            "[${d.severity}] Line ${d.line}: ${d.message}"
        } ?: "No diagnostics found."

        val prompt = "/fix\n\nFile: ${context.filePath}\n\nDiagnostics:\n$diagnosticsText\n\nCode:\n${context.content?.take(2000)}"

        client.sendChatMessage(
            prompt = prompt,
            context = context,
            onComplete = { response ->
                val result = response?.narration ?: "No response from Krnl-AI."
                SwingUtilities.invokeLater {
                    val builder = DialogBuilder(project)
                    builder.title = "/fix — Suggestions"
                    builder.setNorthLabel("Fix suggestions ($diagCount diagnostics):")
                    val textArea = JTextArea(result)
                    textArea.isEditable = false
                    textArea.lineWrap = true
                    textArea.wrapStyleWord = true
                    textArea.rows = 15
                    textArea.columns = 60
                    builder.centerPanel = JScrollPane(textArea)

                    val applyButton = JButton("Apply to Editor")
                    builder.addActionDescriptor(object : com.intellij.openapi.ui.DialogWrapperAction("Apply") {
                        override fun doAction(actionEvent: ActionEvent?) {
                            val currentEditor = CommonDataKeys.EDITOR.getData(e.dataContext) ?: return
                            val document = currentEditor.document
                            com.intellij.openapi.command.WriteCommandAction.runWriteCommandAction(project) {
                                document.insertString(currentEditor.caretModel.offset, "\n// Krnl-AI fix:\n$result\n")
                            }
                            builder.close(DialogBuilder.OK_EXIT_CODE)
                        }
                    })

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
