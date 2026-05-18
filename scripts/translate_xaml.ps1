# Script to translate hardcoded Portuguese strings in XAML files to {loc:Loc key}
# Run from repo root: pwsh scripts\translate_xaml.ps1

$base = "C:\Projects\ia_kernel\krnlai\src\KrnlAI.Desktop.App"

# Mapping: find → replace with {loc:Loc key}
$replacements = @(
    # MainWindow
    @('Text="KERNEL"', 'Text="{loc:Loc sidebar_brand}"'),
    @('Text="Workspace operacional"', 'Text="{loc:Loc sidebar_subtitle}"'),
    @('Text="NAVEGAÇÃO"', 'Text="{loc:Loc sidebar_nav_title}"'),
    @('Text="AGENTE"', 'Text="{loc:Loc sidebar_agent_label}"'),
    @('Text="MODO ATIVO"', 'Text="{loc:Loc sidebar_mode_label}"'),
    @('Text="CONVERSAS"', 'Text="{loc:Loc sidebar_sessions_label}"'),
    @('Text="\+ Nova"', 'Text="{loc:Loc sidebar_new_session}"'),
    @('Text="KrnlAI online"', 'Text="{loc:Loc sidebar_kernel_online}"'),
    @('Text="KrnlAI offline"', 'Text="{loc:Loc sidebar_kernel_offline}"'),
    @('Text="Modo claro"', 'Text="{loc:Loc sidebar_theme_dark}"'),
    @('Text="Modo escuro"', 'Text="{loc:Loc sidebar_theme_light}"'),
    @('Text="Sair"', 'Text="{loc:Loc sidebar_logout}"'),
    @('Text="Chat operacional"', 'Text="{loc:Loc nav_chat}"'),
    @('Text="Saúde e métricas"', 'Text="{loc:Loc nav_dashboard}"'),
    @('Text="Políticas"', 'Text="{loc:Loc nav_policies}"'),
    @('Text="Episódios"', 'Text="{loc:Loc nav_episodes}"'),
    @('Text="Memória"', 'Text="{loc:Loc nav_memory}"'),
    @('Text="Configurações"', 'Text="{loc:Loc nav_settings}"'),
    @('Text="Benchmark"', 'Text="{loc:Loc nav_benchmark}"'),
    @('Text="Grafo Causal"', 'Text="{loc:Loc nav_causal}"'),
    @('Text="Perfil"', 'Text="{loc:Loc nav_profile}"'),
    @('Text="Automático"', 'Text="{loc:Loc mode_auto}"'),
    @('Text="Semiautônomo"', 'Text="{loc:Loc mode_semi}"'),
    @('Text="Manual"', 'Text="{loc:Loc mode_manual}"'),
    @('Text="Krnl-AI Desktop v2.1"', 'Text="{loc:Loc app_title} {loc:Loc app_version}"'),

    # ChatControl
    @('Text="Chat operacional"', 'Text="{loc:Loc chat_title}"'),
    @('Text="Fluxo explícito: intenção, tradução, execução e narrativa final\."', 'Text="{loc:Loc chat_subtitle}"'),
    @('Text="Enviar"', 'Text="{loc:Loc chat_send}"'),
    @('Content="Enviar"', 'Content="{loc:Loc chat_send}"'),
    @('Text="🎤 Voz"', 'Text="{loc:Loc chat_voice}"'),
    @('Text="👤  Usuário"', 'Text="{loc:Loc chat_user_label}"'),
    @('Text="🤖  Krnl-AI"', 'Text="{loc:Loc chat_ai_label}"'),
    @('Text="Gravar áudio"', 'ToolTip="{loc:Loc chat_audio_tooltip_start}"'),
    @('Text="Parar gravação"', 'ToolTip="{loc:Loc chat_audio_tooltip_stop}"'),

    # DashboardControl
    @('Text="Dashboard"', 'Text="{loc:Loc dashboard_title}"'),
    @('Text="Métricas, saúde do sistema e gerenciamento de objetivos\."', 'Text="{loc:Loc dashboard_subtitle}"'),
    @('Text="Atualizar"', 'Text="{loc:Loc dashboard_refresh}"'),
    @('Text="Carregando dashboard\.\.\."', 'Text="{loc:Loc dashboard_loading}"'),
    @('Text="Scorecard de Autonomia"', 'Text="{loc:Loc dashboard_scorecard}"'),
    @('Text="Execuções"', 'Text="{loc:Loc dashboard_metrics_executions}"'),
    @('Text="Sucesso"', 'Text="{loc:Loc dashboard_metrics_success}"'),
    @('Text="Latência Média"', 'Text="{loc:Loc dashboard_metrics_latency}"'),
    @('Text="Custo Estimado"', 'Text="{loc:Loc dashboard_metrics_cost}"'),
    @('Text="Runtime"', 'Text="{loc:Loc dashboard_runtime}"'),
    @('Text="Módulos Ativos"', 'Text="{loc:Loc dashboard_modules}"'),
    @('Text="Autonomia"', 'Text="{loc:Loc dashboard_autonomy}"'),

    # PoliciesControl
    @('Text="Políticas" FontSize="24"', 'Text="{loc:Loc policies_title}" FontSize="24"'),
    @('Text="Políticas, versões e rollbacks\."', 'Text="{loc:Loc policies_subtitle}"'),
    @('Text="Carregando políticas\.\.\."', 'Text="{loc:Loc policies_loading}"'),
    @('Text="Carregando\.\.\."', 'Text="{loc:Loc loading}"'),
    @('Content="Atualizar"', 'Content="{loc:Loc policies_refresh}"'),
    @('Text="Versões" FontSize="16"', 'Text="{loc:Loc policies_versions}" FontSize="16"'),
    @('Text="Rollbacks" FontSize="16"', 'Text="{loc:Loc policies_rollbacks}" FontSize="16"'),
    @('Content="✕ Fechar"', 'Content="{loc:Loc policies_close}"'),

    # EpisodesControl
    @('Text="Episódios" FontSize="24"', 'Text="{loc:Loc episodes_title}" FontSize="24"'),
    @('Text="Histórico de execuções do agente\."', 'Text="{loc:Loc episodes_subtitle}"'),
    @('Text="Carregando episódios\.\.\."', 'Text="{loc:Loc episodes_loading}"'),
    @('Text="Detalhe do Episódio"', 'Text="{loc:Loc episodes_detail}"'),

    # MemoryControl
    @('Text="Memória" FontSize="24"', 'Text="{loc:Loc memory_title}" FontSize="24"'),
    @('Text="Busca semântica, métricas e memória de trabalho\."', 'Text="{loc:Loc memory_subtitle}"'),
    @('Content="Buscar"', 'Content="{loc:Loc memory_search}"'),
    @('Text="Buscando\.\.\."', 'Text="{loc:Loc memory_searching}"'),
    @('Text="Resultados"', 'Text="{loc:Loc memory_results}"'),
    @('Text="🔍 Busca"', 'Text="{loc:Loc memory_search}"'),
    @('Text="📊 Métricas"', 'Text="{loc:Loc memory_metrics}"'),
    @('Text="🧠 Working"', 'Text="{loc:Loc memory_working}"'),
    @('Text="Total requests"', 'Text="{loc:Loc memory_metrics_total}"'),
    @('Text="Fallbacks"', 'Text="{loc:Loc memory_metrics_fallbacks}"'),
    @('Text="Média candidatos"', 'Text="{loc:Loc memory_metrics_candidates}"'),
    @('Text="Média hits"', 'Text="{loc:Loc memory_metrics_hits}"'),
    @('Text="Memória de Trabalho"', 'Text="{loc:Loc memory_working}"'),

    # BenchmarkControl
    @('Text="Benchmark" FontSize="24"', 'Text="{loc:Loc benchmark_title}" FontSize="24"'),
    @('Text="Score, suítes e latência do sistema\."', 'Text="{loc:Loc benchmark_subtitle}"'),
    @('Text="Carregando benchmark\.\.\."', 'Text="{loc:Loc benchmark_loading}"'),
    @('Text="Score Geral"', 'Text="{loc:Loc benchmark_score}"'),
    @('Text="Suítes" FontSize="16"', 'Text="{loc:Loc benchmark_suites}" FontSize="16"'),
    @('Text="Latência média"', 'Text="{loc:Loc benchmark_avg_latency}"'),
    @('Text="Taxa de sucesso"', 'Text="{loc:Loc benchmark_avg_success}"'),

    # CausalGraphControl
    @('Text="Grafo Causal" FontSize="24"', 'Text="{loc:Loc causal_title}" FontSize="24"'),
    @('Text="Relações causais entre eventos e predições\."', 'Text="{loc:Loc causal_subtitle}"'),
    @('Text="🔍 Consulta Causal"', 'Text="{loc:Loc causal_query_tab}"'),
    @('Text="🔮 Predição"', 'Text="{loc:Loc causal_predict_tab}"'),
    @('Text="Grafo de Causas"', 'Text="{loc:Loc causal_result_title}"'),
    @('Text="Resultado da Predição"', 'Text="{loc:Loc causal_prediction_title}"'),
    @('Text="Probabilidade"', 'Text="{loc:Loc causal_probability}"'),

    # ProfileControl
    @('Text="Perfil do Usuário" FontSize="24"', 'Text="{loc:Loc profile_title}" FontSize="24"'),
    @('Text="Suas informações e preferências\."', 'Text="{loc:Loc profile_subtitle}"'),
    @('Text="Identificação"', 'Text="{loc:Loc profile_identification}"'),
    @('Text="User ID"', 'Text="{loc:Loc profile_user_id}"'),
    @('Text="Nome"', 'Text="{loc:Loc profile_name}"'),
    @('Text="Email"', 'Text="{loc:Loc profile_email}"'),
    @('Text="Função"', 'Text="{loc:Loc profile_role}"'),
    @('Content="Recarregar"', 'Content="{loc:Loc profile_reload}"'),
    @('Content="Salvar"', 'Content="{loc:Loc profile_save}"'),

    # SettingsControl
    @('Text="Configurações" FontSize="24"', 'Text="{loc:Loc settings_title}" FontSize="24"'),
    @('Text="Endpoint da API"', 'Text="{loc:Loc settings_api_endpoint}"'),
    @('Text="URL base para conexão com o backend\."', 'Text="{loc:Loc settings_api_desc}"'),
    @('Text="Dispositivos" FontSize="15"', 'Text="{loc:Loc settings_devices}" FontSize="15"'),
    @('Text="Microfone, auto-falante e câmera\."', 'Text="{loc:Loc settings_devices_desc}"'),
    @('Text="Microfone" FontSize="11"', 'Text="{loc:Loc settings_microphone}" FontSize="11"'),
    @('Text="Auto-falante" FontSize="11"', 'Text="{loc:Loc settings_speaker}" FontSize="11"'),
    @('Text="Câmera" FontSize="11"', 'Text="{loc:Loc settings_camera}" FontSize="11"'),
    @('Text="Aparência" FontSize="15"', 'Text="{loc:Loc settings_appearance}" FontSize="15"'),
    @('Text="Escuta Contínua" FontSize="15"', 'Text="{loc:Loc settings_listening}" FontSize="15"'),
    @('Text="Threshold VAD" FontSize="11"', 'Text="{loc:Loc settings_vad}" FontSize="11"'),
    @('Text="Silence timeout \(ms\)" FontSize="11"', 'Text="{loc:Loc settings_silence}" FontSize="11"'),
    @('Text="🌙  Escuro"', 'Text="{loc:Loc settings_dark}"'),
    @('Text="☀️  Claro"', 'Text="{loc:Loc settings_light}"'),
    @('Text="Idioma / Language"', 'Text="{loc:Loc settings_language}"'),

    # Generic button texts
    @('Content="Buscar"', 'Content="{loc:Loc search}"'),
    @('Content="Salvar"', 'Content="{loc:Loc save}"'),
    @('Content="Fechar"', 'Content="{loc:Loc close}"'),
    @('Content="Atualizar"', 'Content="{loc:Loc refresh}"')
)

# Process each XAML file
$files = @(
    "MainWindow.xaml",
    "ChatControl.xaml",
    "DashboardControl.xaml",
    "PoliciesControl.xaml",
    "EpisodesControl.xaml",
    "MemoryControl.xaml",
    "BenchmarkControl.xaml",
    "CausalGraphControl.xaml",
    "ProfileControl.xaml",
    "SettingsControl.xaml"
)

foreach ($file in $files) {
    $path = Join-Path $base "Controls" $file
    if (!(Test-Path $path)) {
        $path = Join-Path $base $file  # Try root of App project
    }
    if (!(Test-Path $path)) {
        Write-Warning "File not found: $file"
        continue
    }

    $content = Get-Content $path -Raw
    $original = $content
    $count = 0

    foreach ($r in $replacements) {
        $old = $r[0]
        $new = $r[1]
        if ($content -match [regex]::Escape($old)) {
            $content = $content -replace [regex]::Escape($old), $new
            $count++
        }
    }

    if ($count -gt 0) {
        Set-Content $path $content -NoNewline
        Write-Host ("✅ " + $file + ": " + $count + " replacements")
    } else {
        Write-Host ("⏭️  " + $file + ": no changes")
    }
}

Write-Host "`nDone!"
