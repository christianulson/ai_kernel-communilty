package com.krnlai.jetbrains.completion

import com.intellij.codeInsight.inline.completion.*
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.krnlai.jetbrains.EarlyStartupActivity
import com.krnlai.jetbrains.client.EditorContextProvider
import com.krnlai.jetbrains.client.KrnlAIClient
import com.krnlai.jetbrains.settings.KrnlAISettings
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withTimeout

class KrnlAICompletionProvider : InlineCompletionProvider {

    private val logger = Logger.getInstance(KrnlAICompletionProvider::class.java)
    private val debounceKey = Key<Long>("KrnlAI.Completion.Debounce")

    override val id: String = "com.krnlai.jetbrains.completion"

    override suspend fun getSuggestion(request: InlineCompletionRequest): InlineCompletionSuggestion? {
        val project = request.project
        val editor = request.editor
        val settings = KrnlAISettings.getInstance()
        if (!settings.enableInlineCompletion) return null

        val client = EarlyStartupActivity.client ?: return null

        val offset = request.startOffset
        val document = editor.document
        val lineStart = document.getLineStartOffset(document.getLineNumber(offset))
        val prefix = document.getText(com.intellij.openapi.util.TextRange(lineStart, offset))

        if (prefix.isBlank() || prefix.length < 3) return null

        ProgressManager.checkCanceled()

        return try {
            val contextProvider = EditorContextProvider(project)
            val context = contextProvider.getContext(editor)
            val prompt = "/complete\n\nContext:\nFile: ${context?.filePath}\nLanguage: ${context?.language}\n\nPrefix:\n$prefix"

            var result: String? = null

            client.sendChatMessage(
                prompt = prompt,
                context = context,
                onComplete = { response ->
                    result = response?.narration
                }
            )

            var waited = 0
            while (result == null && waited < 50) {
                kotlinx.coroutines.delay(100)
                waited++
            }

            result?.let { text ->
                val cleaned = text.trimStart()
                    .removePrefix("```")
                    .removeSuffix("```")
                    .trim()
                if (cleaned.isNotBlank()) {
                    InlinePlainTextSuggestion(cleaned)
                } else null
            }
        } catch (e: Exception) {
            logger.warn("Completion error: ${e.message}")
            null
        }
    }

    override fun provideDisclosureData(
        suggestion: InlineCompletionSuggestion,
        project: Project,
        editor: Editor
    ) = null

    override fun handleEvent(event: InlineCompletionEvent): InlineCompletionHandlerResponse {
        if (event is InlineCompletionEvent.DocumentChange) {
            return InlineCompletionHandlerResponse.HideInlineCompletion
        }
        return InlineCompletionHandlerResponse.PassThrough
    }
}
