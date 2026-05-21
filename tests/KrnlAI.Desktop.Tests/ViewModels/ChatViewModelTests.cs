using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Moq;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ChatViewModelTests
{

    [Fact]
    public void SendMessageCommand_NullOrEmptyText_ShouldNotExecute()
    {
        var vm = new ChatViewModel();
        Assert.False(vm.SendMessageCommand.CanExecute(null));
    }

    [Fact]
    public void InputText_WhenSet_ShouldUpdate()
    {
        var vm = new ChatViewModel();
        vm.InputText = "hello";
        Assert.Equal("hello", vm.InputText);
    }

    [Fact]
    public void InputText_WhenNotEmpty_ShouldEnableSend()
    {
        var vm = new ChatViewModel();
        vm.InputText = "test";
        Assert.True(vm.SendMessageCommand.CanExecute(null));
    }

    [Fact]
    public void ClearChat_ShouldRemoveMessages()
    {
        var vm = new ChatViewModel();
        vm.Messages.Add(new ChatMessage("1", "test", MessageRole.User, DateTime.Now));
        vm.ClearChatCommand.Execute(null);
        Assert.Empty(vm.Messages);
    }

    [Fact]
    public void IsProcessing_WhenTrue_ShouldDisableSend()
    {
        var vm = new ChatViewModel();
        vm.InputText = "test";
        vm.IsProcessing = true;
        Assert.False(vm.SendMessageCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleAudioCapture_ShouldToggleFlag()
    {
        var vm = new ChatViewModel();
        Assert.False(vm.IsCapturingAudio);
    }

    [Fact]
    public void CameraButtonIcon_WhenOff_ShouldShowCamera()
    {
        var vm = new ChatViewModel();
        Assert.False(vm.IsCameraOn);
    }

    [Fact]
    public void Messages_Initially_ShouldBeEmpty()
    {
        var vm = new ChatViewModel();
        Assert.Empty(vm.Messages);
    }
}
