package com.krnlai.jetbrains.settings

import com.intellij.openapi.components.*
import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurableProvider
import com.intellij.openapi.options.SearchableConfigurable
import com.intellij.openapi.ui.DialogPanel
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.components.JBTextField
import com.intellij.ui.dsl.builder.*
import com.intellij.util.xmlb.XmlSerializerUtil
import org.jetbrains.annotations.Nls
import javax.swing.JComponent

@Service
@State(name = "KrnlAISettings", storages = [Storage("krnl-ai-settings.xml")])
class KrnlAISettings : PersistentStateComponent<KrnlAISettings> {

    var sidecarUrl: String = "http://127.0.0.1:5001"
    var apiKey: String = ""
    var authToken: String = ""
    var enableInlineCompletion: Boolean = true
    var enableChat: Boolean = true
    var enableDashboard: Boolean = true
    var enableMemorySearch: Boolean = true

    override fun getState(): KrnlAISettings = this

    override fun loadState(state: KrnlAISettings) {
        XmlSerializerUtil.copyBean(state, this)
    }

    companion object {
        fun getInstance(): KrnlAISettings = com.intellij.openapi.components.ServiceManager.getService(KrnlAISettings::class.java)
    }
}

class KrnlAISettingsConfigurable : SearchableConfigurable, Configurable {

    private val settings = KrnlAISettings.getInstance()
    private var panel: DialogPanel? = null

    override fun getId(): String = "com.krnlai.jetbrains.settings"

    override fun getDisplayName(): String = "Krnl-AI"

    override fun createComponent(): JComponent {
        panel = panel {
            group("Connection") {
                row("Sidecar URL:") {
                    textField()
                        .bindText(settings::sidecarUrl)
                        .columns(30)
                        .comment("e.g. http://127.0.0.1:5001")
                        .focused()
                }
                row("API Key:") {
                    passwordField()
                        .bindText(settings::apiKey)
                        .columns(30)
                }
                row("Auth Token:") {
                    passwordField()
                        .bindText(settings::authToken)
                        .columns(30)
                }
            }
            group("Features") {
                row {
                    checkBox("Enable Inline Completion")
                        .bindSelected(settings::enableInlineCompletion)
                }
                row {
                    checkBox("Enable Chat")
                        .bindSelected(settings::enableChat)
                }
                row {
                    checkBox("Enable Dashboard")
                        .bindSelected(settings::enableDashboard)
                }
                row {
                    checkBox("Enable Memory Search")
                        .bindSelected(settings::enableMemorySearch)
                }
            }
        }
        return panel!!
    }

    override fun isModified(): Boolean {
        return panel?.isModified() ?: false
    }

    override fun apply() {
        panel?.apply()
    }

    override fun reset() {
        panel?.reset()
    }

    override fun disposeUIResources() {
        panel = null
    }
}
