using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class CognitiveStudioControl : UserControl
{
    private Point _dragStart;
    private CognitiveNode? _draggingNode;

    public CognitiveStudioControl() { InitializeComponent(); }

    private void OnPaletteItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.Tag is CognitiveStepType stepType)
        {
            DragDrop.DoDragDrop(this, stepType.ToString(), DragDropEffects.Copy);
        }
    }

    private void OnNodeMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CognitiveNode node)
        {
            if (DataContext is ViewModels.MainViewModel vm) vm.StudioVM.SelectedNode = node;
            _draggingNode = node;
            _dragStart = e.GetPosition(CanvasItems);
            fe.CaptureMouse();
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (_draggingNode != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(CanvasItems);
            var dx = pos.X - _dragStart.X;
            var dy = pos.Y - _dragStart.Y;
            _draggingNode.X += dx;
            _draggingNode.Y += dy;
            _dragStart = pos;
            // Force refresh
            var idx = (DataContext as ViewModels.MainViewModel)?.StudioVM.Nodes.IndexOf(_draggingNode);
            if (idx.HasValue && idx.Value >= 0)
            {
                var vm = (DataContext as ViewModels.MainViewModel)?.StudioVM;
                if (vm != null)
                {
                    vm.Nodes.RemoveAt(idx.Value);
                    vm.Nodes.Insert(idx.Value, _draggingNode);
                }
            }
        }
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseUp(e);
        if (_draggingNode != null)
        {
            var elem = CanvasItems.ItemContainerGenerator.ContainerFromItem(_draggingNode) as UIElement;
            elem?.ReleaseMouseCapture();
            _draggingNode = null;
        }
    }

    private void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.StringFormat) is string stepTypeStr
            && Enum.TryParse<CognitiveStepType>(stepTypeStr, out var stepType)
            && DataContext is ViewModels.MainViewModel vm)
        {
            vm.StudioVM.AddNodeCommand.Execute(stepTypeStr);
        }
    }

    private void OnTemplates(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var t in new[] { ("customer-support", "Customer Support Agent"), ("research-assistant", "Research Assistant"), ("code-reviewer", "Code Reviewer") })
        {
            var item = new MenuItem { Header = t.Item2, Tag = t.Item1 };
            item.Click += (_, _) =>
            {
                if (DataContext is ViewModels.MainViewModel vm)
                    vm.StudioVM.LoadTemplateCommand.Execute(t.Item1);
            };
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }
}
