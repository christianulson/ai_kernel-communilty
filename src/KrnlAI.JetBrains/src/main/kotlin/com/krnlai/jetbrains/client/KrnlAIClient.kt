package com.krnlai.jetbrains.client

import com.google.gson.Gson
import com.google.gson.JsonParser
import com.intellij.openapi.diagnostic.Logger
import com.krnlai.jetbrains.settings.KrnlAISettings
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.IOException
import java.util.concurrent.TimeUnit

data class ChatRequest(val prompt: String, val context: EditorContextDto? = null, val mode: String = "standalone")
data class ChatResponse(val narration: String?, val error: String?, val transportSteps: List<TransportStepDto>?)
data class TransportStepDto(val label: String, val detail: String?, val ok: Boolean)
data class EditorContextDto(
    val filePath: String?,
    val content: String?,
    val selection: String?,
    val caretOffset: Int?,
    val fileExtension: String?,
    val language: String?,
    val diagnostics: List<DiagnosticDto>?
)
data class DiagnosticDto(val line: Int, val column: Int, val severity: String, val message: String, val ruleId: String?)
data class CognitiveDashboardResponse(
    val overallHealth: Double?,
    val activeModules: List<Any>?,
    val recentEvents: List<Any>?,
    val autonomy: Any?
)
data class EmotionalStateResponse(val valence: Double?, val arousal: Double?, val mode: String?)
data class MemorySearchRequest(val query: String, val limit: Int = 10, val topK: Int = 10)
data class MemoryIngestRequest(val content: String, val source: String = "jetbrains")
data class HealthStatus(val status: String?, val mode: String?)

class KrnlAIClient(private val settings: KrnlAISettings) {

    private val logger = Logger.getInstance(KrnlAIClient::class.java)
    private val gson = Gson()
    private val jsonMediaType = "application/json; charset=utf-8".toMediaType()

    private val httpClient: OkHttpClient = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    private val eventListener = object : EventListener() {
        override fun callStart(call: Call) {
            logger.debug("HTTP ${call.request().method} ${call.request().url}")
        }
        override fun callFailed(call: Call, ioe: IOException) {
            logger.warn("HTTP call failed: ${ioe.message}")
        }
    }

    private val authenticatedClient: OkHttpClient = httpClient.newBuilder()
        .addInterceptor { chain ->
            val original = chain.request()
            val builder = original.newBuilder()
            val apiKey = settings.apiKey
            if (apiKey.isNotBlank()) {
                builder.addHeader("X-Api-Key", apiKey)
            }
            val token = settings.authToken
            if (token.isNotBlank()) {
                builder.addHeader("Authorization", "Bearer $token")
            }
            chain.proceed(builder.build())
        }
        .eventListener(eventListener)
        .build()

    private val baseUrl: String
        get() = settings.sidecarUrl.trimEnd('/')

    private fun buildUrl(path: String): String = "$baseUrl$path"

    fun checkHealth(): HealthStatus? {
        return try {
            val request = Request.Builder().url(buildUrl("/health")).get().build()
            val response = authenticatedClient.newCall(request).execute()
            if (response.isSuccessful) {
                val body = response.body?.string() ?: return null
                gson.fromJson(body, HealthStatus::class.java)
            } else null
        } catch (e: Exception) {
            logger.warn("Health check failed: ${e.message}")
            null
        }
    }

    fun sendChatMessage(
        prompt: String,
        context: EditorContextDto? = null,
        onChunk: ((String) -> Unit)? = null,
        onComplete: ((ChatResponse?) -> Unit)? = null
    ) {
        val requestBody = gson.toJson(ChatRequest(prompt = prompt, context = context))
        val request = Request.Builder()
            .url(buildUrl("/agent/run"))
            .post(requestBody.toRequestBody(jsonMediaType))
            .build()

        if (onChunk != null) {
            authenticatedClient.newCall(request).enqueue(object : Callback {
                override fun onFailure(call: Call, e: IOException) {
                    logger.error("Chat request failed", e)
                    onComplete?.invoke(null)
                }

                override fun onResponse(call: Call, response: Response) {
                    response.body?.let { body ->
                        try {
                            val fullJson = body.string()
                            val json = JsonParser.parseString(fullJson).asJsonObject
                            val narration = json.get("narration")?.asString ?: ""
                            narration.split("(?<=\\s)".toRegex()).forEach { word ->
                                onChunk("$word ")
                            }
                            val chatResponse = gson.fromJson(fullJson, ChatResponse::class.java)
                            onComplete?.invoke(chatResponse)
                        } catch (e: Exception) {
                            logger.error("Error parsing chat response", e)
                            onComplete?.invoke(null)
                        }
                    }
                }
            })
        } else {
            try {
                val response = authenticatedClient.newCall(request).execute()
                val body = response.body?.string()
                onComplete?.invoke(body?.let { gson.fromJson(it, ChatResponse::class.java) })
            } catch (e: Exception) {
                logger.error("Chat request failed", e)
                onComplete?.invoke(null)
            }
        }
    }

    fun getDashboard(): CognitiveDashboardResponse? {
        return try {
            val request = Request.Builder().url(buildUrl("/cognitive/dashboard")).get().build()
            val response = authenticatedClient.newCall(request).execute()
            if (response.isSuccessful) {
                response.body?.string()?.let { gson.fromJson(it, CognitiveDashboardResponse::class.java) }
            } else null
        } catch (e: Exception) {
            logger.warn("Failed to fetch dashboard: ${e.message}")
            null
        }
    }

    fun getEmotionalState(): EmotionalStateResponse? {
        return try {
            val request = Request.Builder().url(buildUrl("/emotions/current")).get().build()
            val response = authenticatedClient.newCall(request).execute()
            if (response.isSuccessful) {
                response.body?.string()?.let { gson.fromJson(it, EmotionalStateResponse::class.java) }
            } else null
        } catch (e: Exception) {
            logger.warn("Failed to fetch emotional state: ${e.message}")
            null
        }
    }

    fun searchMemory(query: String, limit: Int = 10): Any? {
        return try {
            val requestBody = gson.toJson(MemorySearchRequest(query = query, limit = limit))
            val request = Request.Builder()
                .url(buildUrl("/memory/search"))
                .post(requestBody.toRequestBody(jsonMediaType))
                .build()
            val response = authenticatedClient.newCall(request).execute()
            if (response.isSuccessful) {
                JsonParser.parseString(response.body?.string())
            } else null
        } catch (e: Exception) {
            logger.warn("Memory search failed: ${e.message}")
            null
        }
    }

    fun ingestMemory(content: String, source: String = "jetbrains"): Boolean {
        return try {
            val requestBody = gson.toJson(MemoryIngestRequest(content = content, source = source))
            val request = Request.Builder()
                .url(buildUrl("/memory/ingest"))
                .post(requestBody.toRequestBody(jsonMediaType))
                .build()
            val response = authenticatedClient.newCall(request).execute()
            response.isSuccessful
        } catch (e: Exception) {
            logger.warn("Memory ingest failed: ${e.message}")
            false
        }
    }

    fun shutdown() {
        httpClient.dispatcher.executorService.shutdown()
        httpClient.connectionPool.evictAll()
    }
}
