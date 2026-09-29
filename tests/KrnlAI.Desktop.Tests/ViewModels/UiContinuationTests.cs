using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class UiContinuationTests
{
    [Fact]
    public void UserServices_LoadAfterDelayedResponse_ShouldUpdateCollectionOnUiThread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            var frame = new DispatcherFrame();
            dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    var response = new TaskCompletionSource<List<UserServiceInfo>>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var client = new Mock<IKernelClient>();
                    client.Setup(c => c.GetUserServicesAsync(It.IsAny<CancellationToken>())).Returns(response.Task);
                    var vm = new UserServicesViewModel(client.Object);
                    var updatesOnUiThread = true;
                    vm.Services.CollectionChanged += (_, _) => updatesOnUiThread &= dispatcher.CheckAccess();
                    var loading = vm.LoadAsync();
                    response.SetResult([new UserServiceInfo("github", true, true, null)]);
                    await loading;
                    Assert.Single(vm.Services);
                    Assert.True(updatesOnUiThread, "Bound collections must be updated on the WPF dispatcher after asynchronous I/O.");
                }
                catch (Exception ex) { failure = ex; }
                finally { frame.Continue = false; }
            });
            Dispatcher.PushFrame(frame);
            dispatcher.InvokeShutdown();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(4)), "UI continuation timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
