using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public enum CognitiveStepType
{
    Sensor, Attention, Memory, Evaluation, MetaCognition,
    Planning, Governance, Execution, Outcome, Learning
}

public class CognitiveNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public CognitiveStepType StepType { get; set; }
    public string Label { get; set; } = "";
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
    public bool Enabled { get; set; } = true;
    public int Timeout { get; set; } = 30;
    public string? SkipCondition { get; set; }
}

public class CognitiveEdge
{
    public string SourceId { get; set; } = "";
    public string TargetId { get; set; } = "";
}

public static class CognitiveStepMeta
{
    public static readonly Dictionary<CognitiveStepType, (string Label, string Color, string Icon)> Meta = new()
    {
        [CognitiveStepType.Sensor] = ("Sensor", "#22c55e", "📥"),
        [CognitiveStepType.Attention] = ("Attention", "#eab308", "🎯"),
        [CognitiveStepType.Memory] = ("Memory", "#a855f7", "🧠"),
        [CognitiveStepType.Evaluation] = ("Evaluation", "#ef4444", "📊"),
        [CognitiveStepType.MetaCognition] = ("MetaCognition", "#3b82f6", "💭"),
        [CognitiveStepType.Planning] = ("Planning", "#f97316", "📋"),
        [CognitiveStepType.Governance] = ("Governance", "#6b7280", "🛡️"),
        [CognitiveStepType.Execution] = ("Execution", "#eab308", "⚡"),
        [CognitiveStepType.Outcome] = ("Outcome", "#22c55e", "📈"),
        [CognitiveStepType.Learning] = ("Learning", "#3b82f6", "📚"),
    };

    public static readonly CognitiveStepType[] StepOrder = [
        CognitiveStepType.Sensor, CognitiveStepType.Attention, CognitiveStepType.Memory,
        CognitiveStepType.Evaluation, CognitiveStepType.MetaCognition, CognitiveStepType.Planning,
        CognitiveStepType.Governance, CognitiveStepType.Execution, CognitiveStepType.Outcome,
        CognitiveStepType.Learning
    ];
}

public class CognitiveStudioViewModel : ViewModelBase
{
    private CognitiveNode? _selectedNode;
    private string _flowName = "New Flow";
    private string _flowDescription = "";
    private string _validationMessage = "Add a Sensor node to begin.";
    private bool _isValid;
    private bool _showDebug;
    private string _debugOutput = "";
    private string _toastMessage = "";

    public ObservableCollection<CognitiveNode> Nodes { get; } = new();
    public ObservableCollection<CognitiveEdge> Edges { get; } = new();
    public ObservableCollection<CognitiveStepType> PaletteItems { get; } = new(CognitiveStepMeta.StepOrder);

    public CognitiveNode? SelectedNode
    {
        get => _selectedNode;
        set { SetProperty(ref _selectedNode, value); OnPropertyChanged(nameof(HasSelection)); }
    }
    public bool HasSelection => _selectedNode != null;
    public string FlowName { get => _flowName; set => SetProperty(ref _flowName, value); }
    public string FlowDescription { get => _flowDescription; set => SetProperty(ref _flowDescription, value); }
    public string ValidationMessage { get => _validationMessage; set => SetProperty(ref _validationMessage, value); }
    public bool IsValid { get => _isValid; set => SetProperty(ref _isValid, value); }
    public bool ShowDebug { get => _showDebug; set => SetProperty(ref _showDebug, value); }
    public string DebugOutput { get => _debugOutput; set => SetProperty(ref _debugOutput, value); }
    public string ToastMessage { get => _toastMessage; set { SetProperty(ref _toastMessage, value); OnPropertyChanged(nameof(HasToast)); } }
    public bool HasToast => !string.IsNullOrEmpty(_toastMessage);

    public ICommand AddNodeCommand { get; }
    public ICommand DeleteNodeCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand ToggleDebugCommand { get; }
    public ICommand RunPreviewCommand { get; }
    public ICommand ClearCanvasCommand { get; }
    public ICommand LoadTemplateCommand { get; }

    public CognitiveStudioViewModel()
    {
        AddNodeCommand = new RelayCommand(p =>
        {
            if (p is string stepType && Enum.TryParse<CognitiveStepType>(stepType, out var st))
                AddNode(st);
        });
        DeleteNodeCommand = new RelayCommand(p =>
        {
            if (p is string id) { var n = Nodes.FirstOrDefault(x => x.Id == id); if (n != null) { Nodes.Remove(n); Edges.Where(e => e.SourceId == id || e.TargetId == id).ToList().ForEach(e => Edges.Remove(e)); } }
            Validate();
        });
        ValidateCommand = new RelayCommand(() => Validate());
        ToggleDebugCommand = new RelayCommand(() => ShowDebug = !ShowDebug);
        RunPreviewCommand = new AsyncRelayCommand(async () =>
        {
            DebugOutput = "";
            foreach (var step in CognitiveStepMeta.StepOrder)
            {
                var node = Nodes.FirstOrDefault(n => n.StepType == step);
                if (node != null)
                {
                    DebugOutput += $"▶ {node.Label} ({node.StepType})...\n";
                    await Task.Delay(300);
                    DebugOutput += $"  ✓ Complete\n";
                }
            }
            DebugOutput += "\n🏁 Preview complete.";
            OnPropertyChanged(nameof(DebugOutput));
        });
        ClearCanvasCommand = new RelayCommand(() => { Nodes.Clear(); Edges.Clear(); Validate(); });
        LoadTemplateCommand = new AsyncRelayCommand(async p =>
        {
            if (p is not string template) return;
            Nodes.Clear(); Edges.Clear();
            var templates = GetTemplates();
            if (templates.TryGetValue(template, out var steps))
            {
                double y = 50;
                CognitiveNode? prev = null;
                foreach (var step in steps)
                {
                    var node = new CognitiveNode { StepType = step, Label = $"{CognitiveStepMeta.Meta[step].Label}-{Nodes.Count + 1}", X = 200, Y = y };
                    Nodes.Add(node);
                    if (prev != null) Edges.Add(new CognitiveEdge { SourceId = prev.Id, TargetId = node.Id });
                    prev = node;
                    y += 120;
                }
                FlowName = template switch { "customer-support" => "Customer Support Agent", "research-assistant" => "Research Assistant", "code-reviewer" => "Code Reviewer", _ => template };
            }
            await Task.CompletedTask;
            Validate();
        });
    }

    private void AddNode(CognitiveStepType type)
    {
        var label = CognitiveStepMeta.Meta[type].Label;
        var count = Nodes.Count(n => n.StepType == type) + 1;
        var node = new CognitiveNode { StepType = type, Label = $"{label}-{count}", X = 200 + Nodes.Count * 30, Y = 100 + Nodes.Count * 40 };
        Nodes.Add(node);
        if (Nodes.Count > 1)
        {
            var prev = Nodes[Nodes.Count - 2];
            Edges.Add(new CognitiveEdge { SourceId = prev.Id, TargetId = node.Id });
        }
        Validate();
    }

    private void Validate()
    {
        if (Nodes.Count == 0) { ValidationMessage = "Add a Sensor node to begin."; IsValid = false; return; }
        if (!Nodes.Any(n => n.StepType == CognitiveStepType.Sensor)) { ValidationMessage = "❌ Flow must include a Sensor node."; IsValid = false; return; }
        if (Nodes.Any(n => string.IsNullOrEmpty(n.Label))) { ValidationMessage = "❌ All nodes must have a label."; IsValid = false; return; }
        IsValid = true;
        ValidationMessage = "✅ Flow is valid";
    }

    private static Dictionary<string, List<CognitiveStepType>> GetTemplates() => new()
    {
        ["customer-support"] = new() { CognitiveStepType.Sensor, CognitiveStepType.Attention, CognitiveStepType.Memory, CognitiveStepType.Evaluation, CognitiveStepType.MetaCognition, CognitiveStepType.Planning, CognitiveStepType.Governance, CognitiveStepType.Execution },
        ["research-assistant"] = new() { CognitiveStepType.Sensor, CognitiveStepType.Planning, CognitiveStepType.Execution, CognitiveStepType.Outcome },
        ["code-reviewer"] = new() { CognitiveStepType.Sensor, CognitiveStepType.Evaluation, CognitiveStepType.MetaCognition, CognitiveStepType.Execution },
    };
}
