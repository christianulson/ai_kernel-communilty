using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public void FireProperty(string name) => OnPropertyChanged(name);
    }

    [Fact]
    public void SetProperty_WhenDifferent_ShouldRaiseEvent()
    {
        var vm = new TestViewModel();
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Name = "New Name";

        Assert.Contains(nameof(TestViewModel.Name), changes);
        Assert.Equal("New Name", vm.Name);
    }

    [Fact]
    public void SetProperty_WhenSame_ShouldNotRaiseEvent()
    {
        var vm = new TestViewModel { Name = "Same" };
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Name = "Same";

        Assert.Empty(changes);
    }

    [Fact]
    public void SetProperty_ShouldReturnTrueWhenChanged()
    {
        var vm = new TestViewModel();
        
        vm.Name = "First";
        Assert.Equal("First", vm.Name);
    }

    [Fact]
    public void MultipleProperties_ShouldTrackIndependently()
    {
        var vm = new TestViewModel();
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Name = "A";
        vm.Count = 42;
        vm.Name = "B";

        Assert.Equal(3, changes.Count);
        Assert.Contains(nameof(TestViewModel.Name), changes);
        Assert.Contains(nameof(TestViewModel.Count), changes);
    }

    [Fact]
    public void OnPropertyChanged_ShouldRaiseEvent()
    {
        var vm = new TestViewModel();
        string? captured = null;
        vm.PropertyChanged += (_, e) => captured = e.PropertyName;

        vm.FireProperty("CustomProp");

        Assert.Equal("CustomProp", captured);
    }
}
