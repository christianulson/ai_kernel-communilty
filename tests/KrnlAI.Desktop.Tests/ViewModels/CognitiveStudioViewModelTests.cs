namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class CognitiveStudioViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new CognitiveStudioViewModel();
        Assert.Equal("New Flow", vm.FlowName);
        Assert.Empty(vm.Nodes);
        Assert.Empty(vm.Edges);
        Assert.False(vm.IsValid);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void AddNodeCommand_WithValidStepType_ShouldAddNode()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        Assert.Single(vm.Nodes);
        Assert.Equal(CognitiveStepType.Sensor, vm.Nodes[0].StepType);
    }

    [Fact]
    public void AddNodeCommand_WithInvalidStepType_ShouldNotAddNode()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("InvalidType");
        Assert.Empty(vm.Nodes);
    }

    [Fact]
    public void AddNode_TwoNodes_ShouldCreateEdge()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        vm.AddNodeCommand.Execute("Attention");
        Assert.Equal(2, vm.Nodes.Count);
        Assert.Single(vm.Edges);
    }

    [Fact]
    public void DeleteNode_ShouldRemoveNodeAndConnectedEdges()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        vm.AddNodeCommand.Execute("Memory");
        var nodeId = vm.Nodes[0].Id;
        vm.DeleteNodeCommand.Execute(nodeId);
        Assert.Single(vm.Nodes);
        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void Validate_NoNodes_ShouldBeInvalid()
    {
        var vm = new CognitiveStudioViewModel();
        Assert.False(vm.IsValid);
        Assert.Contains("Add a Sensor", vm.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithSensorNode_ShouldBeValid()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        vm.ValidateCommand.Execute(null);
        Assert.True(vm.IsValid);
        Assert.Contains("valid", vm.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectNode_ShouldSetSelectedNode()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        var node = vm.Nodes[0];
        vm.SelectedNode = node;
        Assert.True(vm.HasSelection);
        Assert.Same(node, vm.SelectedNode);
    }

    [Fact]
    public void ClearCanvas_ShouldRemoveAllNodesAndEdges()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        vm.AddNodeCommand.Execute("Planning");
        vm.ClearCanvasCommand.Execute(null);
        Assert.Empty(vm.Nodes);
        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void LoadTemplate_ShouldPopulateNodes()
    {
        var vm = new CognitiveStudioViewModel();
        vm.LoadTemplateCommand.Execute("customer-support");
        Assert.NotEmpty(vm.Nodes);
        Assert.True(vm.Nodes.Count >= 3);
    }

    [Fact]
    public void ToggleDebug_ShouldFlipShowDebug()
    {
        var vm = new CognitiveStudioViewModel();
        Assert.False(vm.ShowDebug);
        vm.ToggleDebugCommand.Execute(null);
        Assert.True(vm.ShowDebug);
        vm.ToggleDebugCommand.Execute(null);
        Assert.False(vm.ShowDebug);
    }

    [Fact]
    public void RunPreview_ShouldProduceOutput()
    {
        var vm = new CognitiveStudioViewModel();
        vm.AddNodeCommand.Execute("Sensor");
        vm.AddNodeCommand.Execute("Memory");
        vm.RunPreviewCommand.Execute(null);
        Assert.Contains("▶ Sensor", vm.DebugOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FlowName_ShouldUpdate()
    {
        var vm = new CognitiveStudioViewModel();
        vm.FlowName = "Test Flow";
        Assert.Equal("Test Flow", vm.FlowName);
    }

    [Fact]
    public void FlowDescription_ShouldUpdate()
    {
        var vm = new CognitiveStudioViewModel();
        vm.FlowDescription = "A test flow";
        Assert.Equal("A test flow", vm.FlowDescription);
    }
}
