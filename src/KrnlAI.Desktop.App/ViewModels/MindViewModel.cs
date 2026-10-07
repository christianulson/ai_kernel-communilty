using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>
/// Painel de mente: expõe agenda, notas, metas autônomas, tom emocional e
/// procedimentos aprendidos do kernel embarcado.
/// </summary>
public sealed class MindViewModel : ViewModelBase
{
    private readonly IEmbeddedKrnlAI? _kernel;
    private string _emotionalTone = "neutral";
    private double _valence;
    private double _arousal;
    private int _learnedProcedures;
    private bool _isLoading;
    private string _growthSummary = string.Empty;
    private string _learningFocus = string.Empty;

    public MindViewModel(IEmbeddedKrnlAI? kernel = null)
    {
        _kernel = kernel;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ObservableCollection<string> Agenda { get; } = [];
    public ObservableCollection<string> Notes { get; } = [];
    public ObservableCollection<string> Goals { get; } = [];

    public string EmotionalTone
    {
        get => _emotionalTone;
        private set => SetProperty(ref _emotionalTone, value);
    }

    public double Valence
    {
        get => _valence;
        private set => SetProperty(ref _valence, value);
    }

    public double Arousal
    {
        get => _arousal;
        private set => SetProperty(ref _arousal, value);
    }

    public int LearnedProcedures
    {
        get => _learnedProcedures;
        private set => SetProperty(ref _learnedProcedures, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string GrowthSummary
    {
        get => _growthSummary;
        private set => SetProperty(ref _growthSummary, value);
    }

    /// <summary>
    /// Foco de aprendizado atual (sugestão de currículo ou tendência de evolução), quando disponível.
    /// </summary>
    public string LearningFocus
    {
        get => _learningFocus;
        private set => SetProperty(ref _learningFocus, value);
    }

    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Carrega o snapshot do estado mental do kernel (degrada sem kernel).
    /// </summary>
    public async Task LoadAsync()
    {
        if (_kernel is null)
            return;

        IsLoading = true;
        try
        {
            var snapshot = await _kernel.GetMindSnapshotAsync().ConfigureAwait(true);
            Replace(Agenda, snapshot.UpcomingAgenda);
            Replace(Notes, snapshot.OpenNotes);
            Replace(Goals, snapshot.AutonomousGoals);
            EmotionalTone = snapshot.EmotionalTone;
            Valence = snapshot.Valence;
            Arousal = snapshot.Arousal;
            LearnedProcedures = snapshot.LearnedProcedures;
            GrowthSummary = snapshot.GrowthSummary;
            LearningFocus = snapshot.LearningFocus;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Mind: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void Replace(ObservableCollection<string> target, IReadOnlyList<string> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
