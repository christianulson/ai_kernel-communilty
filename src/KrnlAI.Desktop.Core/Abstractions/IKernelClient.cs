namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Client for communicating with the Krnl-AI backend API.
/// Inherits from <see cref="IBackendApi"/> (core backend methods) and
/// adds WPF-specific methods for desktop UI features.</summary>
public interface IKernelClient : IBackendApi, IAuthClient, IMemoryClient, IPolicyClient, IEpisodeClient, IDashboardClient, IGoalClient, IAdminClient, IKernelAgentClient, IKernelSpeechClient, ISnapshotClient, IObjectiveClient, IInvestigationClient, IApprovalClient
{
    // Coding
    Task<Core.Models.CodingResponse?> CodingExplainAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingResponse?> CodingFixAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingResponse?> CodingGenerateTestsAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingResponse?> CodingReviewAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingResponse?> CodingApplyDiffAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingResponse?> CodingCompleteAsync(Core.Models.CodingRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.CodingStatus?> GetCodingStatusAsync(string cycleId, CancellationToken cancellationToken = default);

    // Self-Improvement
    Task<Core.Models.SelfImprovementStatus?> GetSelfImprovementStatusAsync(CancellationToken cancellationToken = default);

    // Assistant (Threads)
    Task<Core.Models.ThreadInfo?> CreateThreadAsync(string? title = null, CancellationToken cancellationToken = default);
    Task<Core.Models.ThreadInfo?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default);
    Task<Core.Models.MessageInfo?> SendMessageAsync(string threadId, string content, CancellationToken cancellationToken = default);
    Task<List<Core.Models.MessageInfo>> GetMessagesAsync(string threadId, CancellationToken cancellationToken = default);
    Task<Core.Models.RunInfo?> CreateRunAsync(string threadId, CancellationToken cancellationToken = default);
    Task<Core.Models.RunInfo?> GetRunAsync(string threadId, string runId, CancellationToken cancellationToken = default);
    Task<bool> CancelRunAsync(string threadId, string runId, CancellationToken cancellationToken = default);

    // MCP Config
    Task<Core.Models.McpServerConfig?> GetMcpServerConfigAsync(string serverId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMcpServerAsync(string serverId, Core.Models.McpServerConfig config, CancellationToken cancellationToken = default);
    void SetBaseUrl(string baseUrl);
    Task<Core.Models.MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
    Task<Core.Models.PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken cancellationToken = default);
    Task<List<Core.Models.PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken cancellationToken = default);
    Task<Core.Models.GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken cancellationToken = default);
    Task<List<Core.Models.McpServerInfo>> GetMcpServersAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken cancellationToken = default);
    Task<List<Core.Models.DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<Core.Models.DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken cancellationToken = default);
    Task<Core.Models.ArchiveStats?> GetArchiveStatsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.ContractsResponse?> GetContractsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken cancellationToken = default);
    Task<List<Core.Models.McpServerInfo>> GetPluginsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.BenchmarkSummary?> GetSafetyReportAsync(CancellationToken cancellationToken = default);
    Task<List<Core.Models.ScheduledTask>> GetScheduledTasksAsync(CancellationToken cancellationToken = default);
    Task<List<Core.Models.MemoryMoment>> GetMemoryMomentsAsync(int limit = 20, CancellationToken cancellationToken = default);

    // Knowledge
    Task<Core.Models.KnowledgeQueryResult?> KnowledgeAskAsync(string query, CancellationToken cancellationToken = default);
    Task<Core.Models.KnowledgeStats?> KnowledgeStatsAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.KnowledgeLearnResponse?> KnowledgeLearnAsync(string content, string source, string? category = null, CancellationToken cancellationToken = default);

    // PIE (Probabilistic Inference Engine)
    Task<Core.Models.PieInferResponse?> PieInferAsync(string premise, string? context = null, CancellationToken cancellationToken = default);
    Task<Core.Models.PieChainResponse?> PieChainAsync(string initialPremise, int steps = 3, string? context = null, CancellationToken cancellationToken = default);
    Task<Core.Models.PieKnowledgeResponse?> PieKnowledgeAsync(string domain, string fact, double certainty = 1.0, CancellationToken cancellationToken = default);
    Task<Core.Models.PieCoherenceData?> PieCoherenceAsync(CancellationToken cancellationToken = default);
    Task<List<Core.Models.PieTerm>> PieTermsAsync(CancellationToken cancellationToken = default);

    // Emotional history
    Task<List<Core.Models.EmotionalHistoryEntry>> EmotionalHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> EmotionalEventAsync(string @event, string? trigger = null, double? valenceDelta = null, double? arousalDelta = null, CancellationToken cancellationToken = default);

    // Events
    Task<List<Core.Models.EventInfo>> EventsRecentAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<Core.Models.EventDetail?> EventDetailAsync(string eventId, CancellationToken cancellationToken = default);
    Task<List<Core.Models.EventInfo>> EventsByMomentAsync(string momentId, CancellationToken cancellationToken = default);

    // Cognitive Flow (Studio)
    Task<Core.Models.CognitiveFlowResult?> CognitiveFlowExecuteAsync(Core.Models.FlowDefinition flow, CancellationToken cancellationToken = default);
    Task<bool> CognitiveFlowSaveAsync(Core.Models.FlowDefinition flow, CancellationToken cancellationToken = default);
    Task<Core.Models.FlowDefinition?> CognitiveFlowLoadAsync(string flowName, CancellationToken cancellationToken = default);

    // User Services
    Task<List<Core.Models.UserServiceInfo>> GetUserServicesAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateUserServiceAsync(string serviceType, Core.Models.UserServiceUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserServiceAsync(string serviceType, CancellationToken cancellationToken = default);

    // Plan
    Task<Core.Models.PlanExecutionResult?> GetCurrentPlanAsync(CancellationToken cancellationToken = default);
    Task<List<Core.Models.PlanStep>> GetPlanStepsAsync(string planId, CancellationToken cancellationToken = default);

    // Feedback History
    Task<List<Core.Models.FeedbackHistoryEntry>> GetFeedbackHistoryAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.FeedbackAverage?> GetFeedbackAverageAsync(CancellationToken cancellationToken = default);

    // Episodic Memory
    Task<Core.Models.EpisodicMemorySearchResult?> SearchEpisodicMemoryAsync(Core.Models.EpisodicMemorySearchRequest request, CancellationToken cancellationToken = default);

    // Templates
    Task<List<Core.Models.TemplateInfo>> TemplateListAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.TemplateInfo?> TemplateGetAsync(string templateId, CancellationToken cancellationToken = default);
    Task<Core.Models.TemplateInfo?> TemplateCreateAsync(Core.Models.CreateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.TemplateInfo?> TemplateUpdateAsync(string templateId, Core.Models.UpdateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<bool> TemplateDeleteAsync(string templateId, CancellationToken cancellationToken = default);
    Task<Core.Models.TemplateRenderResult?> TemplateRenderAsync(string templateId, Core.Models.RenderTemplateRequest request, CancellationToken cancellationToken = default);

    // Experiments
    Task<List<Core.Models.ExperimentInfo>> ExperimentListAsync(CancellationToken cancellationToken = default);
    Task<Core.Models.ExperimentInfo?> ExperimentStartAsync(Core.Models.StartExperimentRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExperimentCompleteAsync(string experimentId, CancellationToken cancellationToken = default);
    Task<bool> ExperimentRecordMetricAsync(string experimentId, Core.Models.RecordMetricRequest request, CancellationToken cancellationToken = default);
    Task<Core.Models.ExperimentAnalysis?> ExperimentGetAnalysisAsync(string experimentId, CancellationToken cancellationToken = default);
}
