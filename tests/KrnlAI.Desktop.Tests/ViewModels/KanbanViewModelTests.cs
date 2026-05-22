using System.Net;
using System.Net.Http;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class KanbanViewModelTests
{
    private static KanbanService CreateService(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new KanbanService(http, NullLogger<KanbanService>.Instance);
    }

    [Fact]
    public void Constructor_Default_ShouldSetInitialValues()
    {
        var vm = new KanbanViewModel();
        Assert.Equal(10, vm.DaysBack);
        Assert.False(vm.IsLoading);
        Assert.Null(vm.ErrorMessage);
        Assert.Empty(vm.Columns);
    }

    [Fact]
    public void DaysBack_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.DaysBack = 7;
        Assert.Equal(7, vm.DaysBack);
    }

    [Fact]
    public void SelectedDomain_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.SelectedDomain = "tech";
        Assert.Equal("tech", vm.SelectedDomain);
    }

    [Fact]
    public void MinPriority_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.MinPriority = 0.5;
        Assert.Equal(0.5, vm.MinPriority);
    }

    [Fact]
    public void SearchText_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.SearchText = "deploy";
        Assert.Equal("deploy", vm.SearchText);
    }

    [Fact]
    public void IsLoading_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.IsLoading = true;
        Assert.True(vm.IsLoading);
        vm.IsLoading = false;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void ErrorMessage_WhenSet_ShouldUpdateProperty()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, "{}")));
        vm.ErrorMessage = "API error";
        Assert.Equal("API error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenApiReturnsData_ShouldPopulateColumns()
    {
        var json = """
            {"columns":[{"column":"backlog","label":"Backlog","cards":[],"totalCount":0}],"metadata":{"totalGoals":0,"totalColumns":1,"filters":{"daysBack":10,"domain":null,"minPriority":null,"userId":null,"search":null}}}
            """;
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(200, json)));
        await vm.LoadAsync();
        Assert.Single(vm.Columns);
        Assert.Equal("backlog", vm.Columns[0].ColumnKey);
        Assert.Equal("Backlog", vm.Columns[0].Label);
        Assert.False(vm.IsLoading);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenApiFails_ShouldSetError()
    {
        var vm = new KanbanViewModel(CreateService(new MockHttpHandler(500, "")));
        await vm.LoadAsync();
        Assert.Empty(vm.Columns);
        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    private sealed class MockHttpHandler(int statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
