using AutoFixture;
using System.Reflection;
using Cts = KrnlAI.Contracts.Contracts;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NAudio.Wave;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class DesktopServiceLifecycleTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();
    [Fact]
    public async Task ListeningService_StartStop_ShouldManageCaptureLifecycle()
    {
        var capture = new FakeAudioCapture();
        var kernelAgent = new FakeKernelClient();
        var kernelSpeech = kernelAgent;
        var playback = Mock.Of<IAudioPlayback>();
        var sut = new ListeningService(capture, kernelAgent, kernelSpeech, playback, NullLogger<ListeningService>.Instance);

        Assert.False(sut.IsListening);

        await sut.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, capture.StartCalls);
        Assert.True(sut.IsListening);

        await sut.StopListeningAsync();

        Assert.Equal(1, capture.StopCalls);
        Assert.False(sut.IsListening);
    }

    [Fact]
    public async Task ListeningService_ConcurrentSpeech_ShouldProcessOnce()
    {
        var capture = new FakeAudioCapture();
        var kernelAgent = new FakeKernelClient();
        var kernelSpeech = kernelAgent;
        var playback = Mock.Of<IAudioPlayback>();
        var sut = new ListeningService(capture, kernelAgent, kernelSpeech, playback, NullLogger<ListeningService>.Instance);

        await sut.StartListeningAsync(CancellationToken.None);
        SeedSpeechBuffer(sut, 1600);
        SetPrivateField(sut, "_wasSpeaking", true);
        SetPrivateField(sut, "_lastSpeechTime", DateTime.UtcNow.AddSeconds(-5));

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 20),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = CancellationToken.None },
            (_, _) =>
            {
                capture.RaiseVoiceLevelChanged(0f);
                return ValueTask.CompletedTask;
            });

        await Task.Delay(300, CancellationToken.None);

        Assert.Equal(1, kernelAgent.TranscribeCalls);
        Assert.Equal(1, kernelAgent.RunAgentCalls);

        await sut.StopListeningAsync();
    }

    [Fact]
    public void AudioCaptureService_BufferLimit_ShouldDiscardOldestData()
    {
        var sut = new AudioCaptureService(NullLogger<AudioCaptureService>.Instance, maxAudioBufferSize: 8);

        InvokeOnDataAvailable(sut, [1, 2, 3, 4, 5, 6]);
        InvokeOnDataAvailable(sut, [7, 8, 9, 10, 11, 12]);

        var buffer = (List<byte>)GetPrivateField(sut, "_audioBuffer")!;

        Assert.Equal(8, buffer.Count);
        Assert.Equal(new byte[] { 5, 6, 7, 8, 9, 10, 11, 12 }, buffer);
    }

    [Fact]
    public async Task VideoCaptureService_StopCaptureAsync_ShouldAwaitCaptureTask()
    {
        var sut = new VideoCaptureService(NullLogger<VideoCaptureService>.Instance);
        var captureTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        SetPrivateField(sut, "_isCapturing", true);
        SetPrivateField(sut, "_captureTask", captureTask.Task);

        var stopTask = sut.StopCaptureAsync();

        await Task.Delay(100, CancellationToken.None);
        Assert.False(stopTask.IsCompleted);

        captureTask.SetResult();
        await stopTask;

        Assert.False(sut.IsCapturing);
    }

    [Fact]
    public async Task WebRtcService_Dispose_ShouldCancelPendingConnectionTransition()
    {
        var sut = new WebRtcService(NullLogger<WebRtcService>.Instance);
        await sut.InitializeAsync("ws://localhost/signaling", "stun.local");
        var connectTask = sut.ConnectToPeerAsync("peer-1");

        sut.Dispose();

        await connectTask;
        await Task.Delay(600, CancellationToken.None);

        Assert.False(sut.IsConnected);
    }

    private static void SeedSpeechBuffer(ListeningService service, int bytes)
    {
        var buffer = (List<byte>)GetPrivateField(service, "_audioBuffer")!;
        buffer.Clear();
        buffer.AddRange(Enumerable.Range(0, bytes).Select(i => (byte)(i % 256)));
    }

    private static object? GetPrivateField(object instance, string fieldName)
        => instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field not found: {fieldName}");
        field.SetValue(instance, value);
    }

    private static void InvokeOnDataAvailable(AudioCaptureService service, byte[] bytes)
    {
        var args = new WaveInEventArgs(bytes, bytes.Length);
        var method = typeof(AudioCaptureService).GetMethod("OnDataAvailable", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OnDataAvailable not found");
        method.Invoke(service, [null, args]);
    }

    private sealed class FakeAudioCapture : IAudioCapture
    {
        public event EventHandler<float>? VoiceLevelChanged;

        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public bool IsCapturing { get; private set; }

        public Task StartCaptureAsync(string? deviceId = null)
        {
            StartCalls++;
            IsCapturing = true;
            return Task.CompletedTask;
        }

        public Task StopCaptureAsync()
        {
            StopCalls++;
            IsCapturing = false;
            return Task.CompletedTask;
        }

        public Task<byte[]> StopCaptureAndGetAudioAsync()
        {
            StopCalls++;
            IsCapturing = false;
            return Task.FromResult(Array.Empty<byte>());
        }

        public IReadOnlyList<MediaDevice> GetAvailableDevices()
            => [new MediaDevice("0", "Default Microphone", MediaDeviceType.AudioInput)];

        public void RaiseVoiceLevelChanged(float level) => VoiceLevelChanged?.Invoke(this, level);

        public void Dispose()
        {
            IsCapturing = false;
        }
    }

    private sealed class FakeKernelClient : IKernelClient, IKernelAgentClient, IKernelSpeechClient
    {
        public int TranscribeCalls { get; private set; }
        public int RunAgentCalls { get; private set; }

        public Task<Cts.AgentRunTransportResponse> RunAgentAsync(Cts.AgentRunTransportRequest request, CancellationToken cancellationToken = default)
        {
            RunAgentCalls++;
            return Task.FromResult(new Cts.AgentRunTransportResponse(null, null, null, null, null));
        }

        public Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<byte>());

        public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
        {
            TranscribeCalls++;
            return Task.FromResult<string?>("speech");
        }

        public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) => Task.FromResult(default(LoginResponse)!);
        public Task<PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => Task.FromResult(default(PolicyListResponse)!);
        public Task<PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default) => Task.FromResult<PolicyDetails?>(null);
        public Task<PolicyInfo?> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default) => Task.FromResult<PolicyInfo?>(null);
        public Task<PolicyInfo?> UpdatePolicyAsync(string policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default) => Task.FromResult<PolicyInfo?>(null);
        public Task<bool> DeletePolicyAsync(string policyId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<EpisodeSearchResult> SearchEpisodesAsync(EpisodeSearchRequest request, CancellationToken cancellationToken = default) => Task.FromResult(default(EpisodeSearchResult)!);
        public Task<EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default) => Task.FromResult<EpisodeDetails?>(null);
        public Task<MemorySearchResult> SearchMemoryAsync(string query, int topK = 10, CancellationToken cancellationToken = default) => Task.FromResult(default(MemorySearchResult)!);
        public Task<MemoryIngestResult> IngestMemoryAsync(MemoryIngestRequest request, CancellationToken cancellationToken = default) => Task.FromResult(default(MemoryIngestResult)!);
        public Task<MemoryMetrics?> GetMemoryMetricsAsync(CancellationToken cancellationToken = default) => Task.FromResult<MemoryMetrics?>(null);
        public Task<WorkingMemorySummary?> GetWorkingMemoryAsync(CancellationToken cancellationToken = default) => Task.FromResult<WorkingMemorySummary?>(null);
        public Task<AgentMetricsSummary?> GetMetricsSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult<AgentMetricsSummary?>(null);
        public Task<AgentScorecard?> GetScorecardAsync(CancellationToken cancellationToken = default) => Task.FromResult<AgentScorecard?>(null);
        public Task<RuntimeSummary?> GetRuntimeSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult<RuntimeSummary?>(null);
        public Task<GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default) => Task.FromResult(default(GoalListResponse)!);
        public Task<GoalDetails?> GetGoalAsync(string goalId, CancellationToken cancellationToken = default) => Task.FromResult<GoalDetails?>(null);
        public Task<GoalInfo?> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default) => Task.FromResult<GoalInfo?>(null);
        public Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<FeedbackResponse> SubmitFeedbackAsync(FeedbackRequest request, CancellationToken cancellationToken = default) => Task.FromResult(default(FeedbackResponse)!);
        public Task<CognitiveDashboardData?> GetCognitiveDashboardAsync(CancellationToken cancellationToken = default) => Task.FromResult<CognitiveDashboardData?>(null);
        public Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<UserProfile?>(null);
        public Task<bool> UpdateUserProfileAsync(UserProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<MultimodalSearchResult?> SearchMultimodalAsync(string query, int topK = 10, CancellationToken cancellationToken = default) => Task.FromResult<MultimodalSearchResult?>(null);
        public Task<BenchmarkSummary?> GetBenchmarkSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult<BenchmarkSummary?>(null);
        public Task<CausalQueryResult?> GetCausalQueryAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult<CausalQueryResult?>(null);
        public Task<CausalPrediction?> GetCausalPredictionAsync(string action, CancellationToken cancellationToken = default) => Task.FromResult<CausalPrediction?>(null);
        public Task<CrossSummaryData?> GetCrossSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult<CrossSummaryData?>(null);
        public Task<MetricsByGoalData?> GetMetricsByGoalAsync(CancellationToken cancellationToken = default) => Task.FromResult<MetricsByGoalData?>(null);
        public Task<PolicyVersionList?> GetPolicyVersionsAsync(string policyId, CancellationToken cancellationToken = default) => Task.FromResult<PolicyVersionList?>(null);
        public Task<List<PolicyRollbackEntry>> GetPolicyRollbacksAsync(string policyId, CancellationToken cancellationToken = default) => Task.FromResult(new List<PolicyRollbackEntry>());
        public Task<GoalCycleList?> GetGoalCyclesAsync(string goalId, CancellationToken cancellationToken = default) => Task.FromResult<GoalCycleList?>(null);
        public Task<EmotionalState?> GetEmotionalStateAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<EmotionalState?>(null);
        public Task<AffectiveState?> GetAffectiveStateAsync(CancellationToken cancellationToken = default) => Task.FromResult<AffectiveState?>(null);
        public void SetBaseUrl(string baseUrl) { }
        public void SetAuthToken(string? token) { }
        public void SetTokens(string? token, string? refreshToken) { }

        public Task<List<McpServerInfo>> GetMcpServersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<McpServerInfo>());
        }

        public Task<bool> ToggleMcpServerAsync(string serverId, bool enabled, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<List<Core.Models.DocumentInfo>> GetDocumentsAsync(int limit = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<Core.Models.DocumentInfo>());

        public Task<Core.Models.DocumentInfo?> GetDocumentStatusAsync(string documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.DocumentInfo?>(null);

        public Task<Core.Models.ArchiveStats?> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.ArchiveStats?>(null);

        public Task<Core.Models.VersionsInfo?> GetVersionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.VersionsInfo?>(null);

        public Task<Core.Models.ContractsResponse?> GetContractsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.ContractsResponse?>(null);

        public Task<Core.Models.ModelRegistryDetail?> GetModelRegistryAsync(string modelId, CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.ModelRegistryDetail?>(null);

        public Task<Core.Models.ShareListResponse?> GetSharesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Models.ShareListResponse?>(null);

        public Task<List<Core.Models.SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.SnapshotInfo>());
        public Task<List<Core.Models.ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.ObjectiveInfo>());
        public Task<Core.Models.ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default) => Task.FromResult<Core.Models.ObjectiveDetail?>(null);
        public Task<List<Core.Models.InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.InvestigationInfo>());
        public Task<List<Core.Models.McpServerInfo>> GetPluginsAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.McpServerInfo>());
        public Task<Core.Models.BenchmarkSummary?> GetSafetyReportAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.BenchmarkSummary?>(null);
        public Task<List<Core.Models.ScheduledTask>> GetScheduledTasksAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.ScheduledTask>());
        public Task<List<Core.Models.MemoryMoment>> GetMemoryMomentsAsync(int limit = 20, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.MemoryMoment>());
        public Task<List<Core.Models.ApprovalRequest>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.ApprovalRequest>());
        public Task<Core.Models.ApprovalRequest?> GetApprovalDetailAsync(string requestId, CancellationToken ct = default) => Task.FromResult<Core.Models.ApprovalRequest?>(null);
        public Task<Core.Models.ApprovalRequest?> ApproveRequestAsync(string requestId, string? comment = null, CancellationToken ct = default) => Task.FromResult<Core.Models.ApprovalRequest?>(null);
        public Task<Core.Models.ApprovalRequest?> RejectRequestAsync(string requestId, string? comment = null, CancellationToken ct = default) => Task.FromResult<Core.Models.ApprovalRequest?>(null);
        public Task<Core.Models.KnowledgeQueryResult?> KnowledgeAskAsync(string query, CancellationToken ct = default) => Task.FromResult<Core.Models.KnowledgeQueryResult?>(null);
        public Task<Core.Models.KnowledgeStats?> KnowledgeStatsAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.KnowledgeStats?>(null);
        public Task<Core.Models.KnowledgeLearnResponse?> KnowledgeLearnAsync(string content, string source, string? category = null, CancellationToken ct = default) => Task.FromResult<Core.Models.KnowledgeLearnResponse?>(null);
        public Task<Core.Models.PieInferResponse?> PieInferAsync(string premise, string? context = null, CancellationToken ct = default) => Task.FromResult<Core.Models.PieInferResponse?>(null);
        public Task<Core.Models.PieChainResponse?> PieChainAsync(string initialPremise, int steps = 3, string? context = null, CancellationToken ct = default) => Task.FromResult<Core.Models.PieChainResponse?>(null);
        public Task<Core.Models.PieKnowledgeResponse?> PieKnowledgeAsync(string domain, string fact, double certainty = 1.0, CancellationToken ct = default) => Task.FromResult<Core.Models.PieKnowledgeResponse?>(null);
        public Task<Core.Models.PieCoherenceData?> PieCoherenceAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.PieCoherenceData?>(null);
        public Task<List<Core.Models.PieTerm>> PieTermsAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.PieTerm>());
        public Task<List<Core.Models.EmotionalHistoryEntry>> EmotionalHistoryAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.EmotionalHistoryEntry>());
        public Task<bool> EmotionalEventAsync(string @event, string? trigger = null, double? valenceDelta = null, double? arousalDelta = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<Core.Models.EventInfo>> EventsRecentAsync(int take = 50, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.EventInfo>());
        public Task<Core.Models.EventDetail?> EventDetailAsync(string eventId, CancellationToken ct = default) => Task.FromResult<Core.Models.EventDetail?>(null);
        public Task<List<Core.Models.EventInfo>> EventsByMomentAsync(string momentId, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.EventInfo>());
        public Task<Core.Models.CodingResponse?> CodingExplainAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingResponse?> CodingFixAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingResponse?> CodingGenerateTestsAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingResponse?> CodingReviewAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingResponse?> CodingApplyDiffAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingResponse?> CodingCompleteAsync(Core.Models.CodingRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingResponse?>(null);
        public Task<Core.Models.CodingStatus?> GetCodingStatusAsync(string cycleId, CancellationToken ct = default) => Task.FromResult<Core.Models.CodingStatus?>(null);
        public Task<Core.Models.SelfImprovementStatus?> GetSelfImprovementStatusAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.SelfImprovementStatus?>(null);
        public Task<Core.Models.ThreadInfo?> CreateThreadAsync(string? title = null, CancellationToken ct = default) => Task.FromResult<Core.Models.ThreadInfo?>(null);
        public Task<Core.Models.ThreadInfo?> GetThreadAsync(string threadId, CancellationToken ct = default) => Task.FromResult<Core.Models.ThreadInfo?>(null);
        public Task<Core.Models.MessageInfo?> SendMessageAsync(string threadId, string content, CancellationToken ct = default) => Task.FromResult<Core.Models.MessageInfo?>(null);
        public Task<List<Core.Models.MessageInfo>> GetMessagesAsync(string threadId, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.MessageInfo>());
        public Task<Core.Models.RunInfo?> CreateRunAsync(string threadId, CancellationToken ct = default) => Task.FromResult<Core.Models.RunInfo?>(null);
        public Task<Core.Models.RunInfo?> GetRunAsync(string threadId, string runId, CancellationToken ct = default) => Task.FromResult<Core.Models.RunInfo?>(null);
        public Task<bool> CancelRunAsync(string threadId, string runId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Core.Models.McpServerConfig?> GetMcpServerConfigAsync(string serverId, CancellationToken ct = default) => Task.FromResult<Core.Models.McpServerConfig?>(null);
        public Task<bool> UpdateMcpServerAsync(string serverId, Core.Models.McpServerConfig config, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Core.Models.CognitiveFlowResult?> CognitiveFlowExecuteAsync(Core.Models.FlowDefinition flow, CancellationToken ct = default) => Task.FromResult<Core.Models.CognitiveFlowResult?>(null);
        public Task<bool> CognitiveFlowSaveAsync(Core.Models.FlowDefinition flow, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Core.Models.FlowDefinition?> CognitiveFlowLoadAsync(string flowName, CancellationToken ct = default) => Task.FromResult<Core.Models.FlowDefinition?>(null);
        public Task<List<Core.Models.UserServiceInfo>> GetUserServicesAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.UserServiceInfo>());
        public Task<bool> UpdateUserServiceAsync(string serviceType, Core.Models.UserServiceUpdateRequest request, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteUserServiceAsync(string serviceType, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<Core.Models.TemplateInfo>> TemplateListAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.TemplateInfo>());
        public Task<Core.Models.TemplateInfo?> TemplateGetAsync(string templateId, CancellationToken ct = default) => Task.FromResult<Core.Models.TemplateInfo?>(null);
        public Task<Core.Models.TemplateInfo?> TemplateCreateAsync(Core.Models.CreateTemplateRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.TemplateInfo?>(null);
        public Task<Core.Models.TemplateInfo?> TemplateUpdateAsync(string templateId, Core.Models.UpdateTemplateRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.TemplateInfo?>(null);
        public Task<bool> TemplateDeleteAsync(string templateId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Core.Models.TemplateRenderResult?> TemplateRenderAsync(string templateId, Core.Models.RenderTemplateRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.TemplateRenderResult?>(null);
        public Task<List<Core.Models.ExperimentInfo>> ExperimentListAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.ExperimentInfo>());
        public Task<Core.Models.ExperimentInfo?> ExperimentStartAsync(Core.Models.StartExperimentRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.ExperimentInfo?>(null);
        public Task<bool> ExperimentCompleteAsync(string experimentId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExperimentRecordMetricAsync(string experimentId, Core.Models.RecordMetricRequest request, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Core.Models.ExperimentAnalysis?> ExperimentGetAnalysisAsync(string experimentId, CancellationToken ct = default) => Task.FromResult<Core.Models.ExperimentAnalysis?>(null);
        public Task<Core.Models.PlanExecutionResult?> GetCurrentPlanAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.PlanExecutionResult?>(null);
        public Task<List<Core.Models.PlanStep>> GetPlanStepsAsync(string planId, CancellationToken ct = default) => Task.FromResult(new List<Core.Models.PlanStep>());
        public Task<List<Core.Models.FeedbackHistoryEntry>> GetFeedbackHistoryAsync(CancellationToken ct = default) => Task.FromResult(new List<Core.Models.FeedbackHistoryEntry>());
        public Task<Core.Models.FeedbackAverage?> GetFeedbackAverageAsync(CancellationToken ct = default) => Task.FromResult<Core.Models.FeedbackAverage?>(null);
        public Task<Core.Models.EpisodicMemorySearchResult?> SearchEpisodicMemoryAsync(Core.Models.EpisodicMemorySearchRequest request, CancellationToken ct = default) => Task.FromResult<Core.Models.EpisodicMemorySearchResult?>(null);
    }
}
