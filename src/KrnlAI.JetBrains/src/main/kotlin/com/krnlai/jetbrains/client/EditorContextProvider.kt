package com.krnlai.jetbrains.client

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.*
import com.intellij.psi.util.PsiUtilBase

class EditorContextProvider(private val project: Project) {

    fun getContext(editor: Editor): EditorContextDto? {
        val file = FileDocumentManager.getInstance().getFile(editor.document) ?: return null
        val psiFile = PsiUtilBase.getPsiFile(project, file) ?: return null

        val filePath = file.path
        val content = editor.document.text
        val fileExtension = file.extension
        val language = psiFile.language.displayName

        val caretModel = editor.caretModel
        val selection = caretModel.currentCaret
        val selectedText = selection.selectedText
        val caretOffset = selection.offset

        val diagnostics = getDiagnostics(psiFile, file)

        return EditorContextDto(
            filePath = filePath,
            content = content,
            selection = selectedText,
            caretOffset = caretOffset,
            fileExtension = fileExtension,
            language = language,
            diagnostics = diagnostics
        )
    }

    private fun getDiagnostics(psiFile: PsiFile, file: VirtualFile): List<DiagnosticDto> {
        val result = mutableListOf<DiagnosticDto>()

        val annotationHolder = PsiManager.getInstance(project)
            .findFile(file)
            ?.let { PsiUtilBase.getPsiFile(project, file) }

        if (annotationHolder is PsiJavaFile || annotationHolder is PsiFile) {
            val fileViewProvider = annotationHolder?.viewProvider
            if (fileViewProvider != null) {
                for (language in fileViewProvider.languages) {
                    val psiRoot = fileViewProvider.getPsi(language)
                    psiRoot?.accept(object : PsiRecursiveElementVisitor() {
                        override fun visitElement(element: PsiElement) {
                            super.visitElement(element)
                            for (child in element.children) {
                                if (child is PsiErrorElement) {
                                    result.add(
                                        DiagnosticDto(
                                            line = 0,
                                            column = 0,
                                            severity = "ERROR",
                                            message = child.errorDescription,
                                            ruleId = null
                                        )
                                    )
                                }
                            }
                        }
                    })
                }
            }
        }

        return result
    }

    fun getFileExtension(): String? {
        val editor = getActiveEditor() ?: return null
        val file = FileDocumentManager.getInstance().getFile(editor.document) ?: return null
        return file.extension
    }

    fun getSelectedText(): String? {
        val editor = getActiveEditor() ?: return null
        val selection = editor.caretModel.currentCaret
        return selection.selectedText
    }

    private fun getActiveEditor(): Editor? {
        val fileEditorManager = com.intellij.openapi.fileEditor.FileEditorManager.getInstance(project)
        val editors = fileEditorManager.selectedTextEditor ?: return null
        return editors
    }
}
