package com.krnlai.jetbrains.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.DialogBuilder
import com.intellij.openapi.ui.Messages
import com.intellij.psi.*
import com.intellij.psi.util.PsiUtilBase
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.EditorContextProvider
import javax.swing.*

class TestAction : AnAction() {

    private val logger = Logger.getInstance(TestAction::class.java)

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val client = EarlyStartupActivity.client ?: return

        val file = com.intellij.openapi.fileEditor.FileDocumentManager.getInstance()
            .getFile(editor.document) ?: return
        val psiFile = PsiUtilBase.getPsiFile(project, file) ?: return

        val contextProvider = EditorContextProvider(project)
        val context = contextProvider.getContext(editor)

        val className = findClassName(psiFile)
        val methodName = findMethodName(psiFile, editor.caretModel.offset)

        val prompt = buildString {
            appendLine("/test")
            appendLine("Class: $className")
            appendLine("Method: $methodName")
            appendLine("Language: ${context?.language ?: "unknown"}")
            appendLine("File: ${context?.filePath ?: "unknown"}")
            appendLine()
            appendLine("```${context?.fileExtension ?: ""}")
            appendLine(context?.content?.take(3000) ?: "")
            appendLine("```")
            appendLine()
            appendLine("Generate unit tests for the $className class")
            if (methodName != null) appendLine(" focusing on the $methodName method")
            appendLine(" using the appropriate test framework")
        }

        client.sendChatMessage(
            prompt = prompt,
            context = context,
            onComplete = { response ->
                val result = response?.narration ?: "No response from Krnl-AI."
                SwingUtilities.invokeLater {
                    val builder = DialogBuilder(project)
                    builder.title = "/test — Generated Tests"
                    builder.setNorthLabel("Tests for ${className ?: "current class"}" +
                            if (methodName != null) " method: $methodName" else "")

                    val textArea = JTextArea(result)
                    textArea.isEditable = false
                    textArea.lineWrap = true
                    textArea.wrapStyleWord = true
                    textArea.rows = 20
                    textArea.columns = 70

                    val font = java.awt.Font("JetBrains Mono", java.awt.Font.PLAIN, 12)
                    textArea.font = font

                    builder.centerPanel = JScrollPane(textArea)
                    builder.addCloseButton()
                    builder.show()
                }
            }
        )
    }

    private fun findClassName(psiFile: PsiFile): String? {
        psiFile.accept(object : PsiRecursiveElementVisitor() {
            override fun visitClass(aClass: PsiClass) {
                if (aClass.name != null) {
                    className = aClass.name
                }
                super.visitClass(aClass)
            }
        })
        return className
    }

    private fun findMethodName(psiFile: PsiFile, offset: Int): String? {
        val element = psiFile.findElementAt(offset) ?: return null
        val method = PsiTreeUtil.getParentOfType(element, PsiMethod::class.java) ?: return null
        return method.name
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isEnabledAndVisible = project != null && editor != null
    }

    companion object {
        private var className: String? = null
    }
}
