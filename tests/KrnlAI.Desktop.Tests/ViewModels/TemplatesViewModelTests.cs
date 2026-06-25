using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class TemplatesViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new TemplatesViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.FilteredTemplates);
        Assert.Equal(["Todos"], vm.Categories);
        Assert.Equal("Todos", vm.SelectedCategory);
    }

    [Fact]
    public void Constructor_WithKernelClient_ShouldInitializeProperties()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new TemplatesViewModel(kernelClient.Object);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.FilteredTemplates);
    }

    [Fact]
    public async Task LoadTemplatesAsync_ShouldPopulateTemplates()
    {
        var kernelClient = new Mock<IKernelClient>();
        var templates = new List<TemplateInfo>
        {
            new("t1", "Template 1", "Desc 1", "Content 1", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow),
            new("t2", "Template 2", "Desc 2", "Content 2", "coding", "1.0", DateTime.UtcNow, DateTime.UtcNow),
        };
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var vm = new TemplatesViewModel(kernelClient.Object);
        await vm.LoadTemplatesAsync().ConfigureAwait(false);

        Assert.Equal(2, vm.FilteredTemplates.Count);
        Assert.Equal("Template 1", vm.FilteredTemplates[0].Name);
        Assert.Equal("Template 2", vm.FilteredTemplates[1].Name);
    }

    [Fact]
    public async Task LoadTemplatesAsync_ShouldSetIsLoading()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<TemplateInfo>>();
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new TemplatesViewModel(kernelClient.Object);
        var loadTask = vm.LoadTemplatesAsync();

        Assert.True(vm.IsLoading);
        tcs.SetResult([]);
        await loadTask.ConfigureAwait(false);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadTemplatesAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var vm = new TemplatesViewModel(kernelClient.Object);
        await vm.LoadTemplatesAsync().ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Contains("API error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadTemplatesAsync_ShouldPopulateCategories()
    {
        var kernelClient = new Mock<IKernelClient>();
        var templates = new List<TemplateInfo>
        {
            new("t1", "T1", "", "", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow),
            new("t2", "T2", "", "", "coding", "1.0", DateTime.UtcNow, DateTime.UtcNow),
            new("t3", "T3", "", "", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow),
        };
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var vm = new TemplatesViewModel(kernelClient.Object);
        await vm.LoadTemplatesAsync().ConfigureAwait(false);

        Assert.Contains("Todos", vm.Categories);
        Assert.Contains("general", vm.Categories);
        Assert.Contains("coding", vm.Categories);
        Assert.Equal(3, vm.Categories.Count);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldAddTemplateToList()
    {
        var kernelClient = new Mock<IKernelClient>();
        var created = new TemplateInfo("t-new", "New T", "New desc", "content", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow);
        kernelClient.Setup(k => k.TemplateCreateAsync(It.IsAny<CreateTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([created]);

        var vm = new TemplatesViewModel(kernelClient.Object);
        vm.NewTemplateName = "New T";
        vm.NewTemplateDescription = "New desc";
        vm.NewTemplateContent = "content";
        vm.NewTemplateCategory = "general";

        await vm.CreateTemplateAsync().ConfigureAwait(false);

        Assert.Single(vm.FilteredTemplates);
        Assert.Equal("New T", vm.FilteredTemplates[0].Name);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenNameEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();

        var vm = new TemplatesViewModel(kernelClient.Object);
        vm.NewTemplateName = "";
        vm.NewTemplateDescription = "desc";
        vm.NewTemplateContent = "content";

        await vm.CreateTemplateAsync().ConfigureAwait(false);

        kernelClient.Verify(k => k.TemplateCreateAsync(It.IsAny<CreateTemplateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.TemplateCreateAsync(It.IsAny<CreateTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("create error"));

        var vm = new TemplatesViewModel(kernelClient.Object);
        vm.NewTemplateName = "Test";
        vm.NewTemplateContent = "content";

        await vm.CreateTemplateAsync().ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Contains("create error", vm.ErrorMessage);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldClearFormOnSuccess()
    {
        var kernelClient = new Mock<IKernelClient>();
        var created = new TemplateInfo("t-new", "Test", "desc", "content", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow);
        kernelClient.Setup(k => k.TemplateCreateAsync(It.IsAny<CreateTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = new TemplatesViewModel(kernelClient.Object);
        vm.NewTemplateName = "Test";
        vm.NewTemplateDescription = "desc";
        vm.NewTemplateContent = "content";
        vm.NewTemplateCategory = "general";

        await vm.CreateTemplateAsync().ConfigureAwait(false);

        Assert.Empty(vm.NewTemplateName);
        Assert.Empty(vm.NewTemplateDescription);
        Assert.Empty(vm.NewTemplateContent);
        Assert.Equal("general", vm.NewTemplateCategory);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldRemoveTemplate()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.TemplateDeleteAsync("t1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = new TemplatesViewModel(kernelClient.Object);

        await vm.DeleteTemplateAsync("t1").ConfigureAwait(false);

        Assert.Empty(vm.FilteredTemplates);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.TemplateDeleteAsync("t1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("delete error"));

        var vm = new TemplatesViewModel(kernelClient.Object);

        await vm.DeleteTemplateAsync("t1").ConfigureAwait(false);

        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldSetRenderedContent()
    {
        var kernelClient = new Mock<IKernelClient>();
        var rendered = new TemplateRenderResult("rendered output", null);
        kernelClient.Setup(k => k.TemplateRenderAsync("t1", It.IsAny<RenderTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rendered);

        var vm = new TemplatesViewModel(kernelClient.Object);

        await vm.RenderTemplateAsync("t1").ConfigureAwait(false);

        Assert.Equal("rendered output", vm.RenderedContent);
    }

    [Fact]
    public async Task RenderTemplateAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.TemplateRenderAsync("t1", It.IsAny<RenderTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("render error"));

        var vm = new TemplatesViewModel(kernelClient.Object);

        await vm.RenderTemplateAsync("t1").ConfigureAwait(false);

        Assert.True(vm.HasError);
        Assert.Null(vm.RenderedContent);
    }

    [Fact]
    public async Task FilterByCategory_ShouldFilterTemplates()
    {
        var kernelClient = new Mock<IKernelClient>();
        var templates = new List<TemplateInfo>
        {
            new("t1", "T1", "", "", "general", "1.0", DateTime.UtcNow, DateTime.UtcNow),
            new("t2", "T2", "", "", "coding", "1.0", DateTime.UtcNow, DateTime.UtcNow),
        };
        kernelClient.Setup(k => k.TemplateListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var vm = new TemplatesViewModel(kernelClient.Object);
        await vm.LoadTemplatesAsync().ConfigureAwait(false);

        Assert.Equal(2, vm.FilteredTemplates.Count);

        vm.FilterByCategoryCommand.Execute("coding");

        Assert.Single(vm.FilteredTemplates);
        Assert.Equal("T2", vm.FilteredTemplates[0].Name);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new TemplatesViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearRenderedContent_ShouldClearRenderedContent()
    {
        var vm = new TemplatesViewModel();
        vm.RenderedContent = "some content";

        vm.ClearRenderedContent();

        Assert.Null(vm.RenderedContent);
    }

    [Fact]
    public void LoadTemplatesCommand_ShouldExist()
    {
        var vm = new TemplatesViewModel();
        Assert.NotNull(vm.LoadTemplatesCommand);
    }

    [Fact]
    public void CreateTemplateCommand_ShouldExist()
    {
        var vm = new TemplatesViewModel();
        Assert.NotNull(vm.CreateTemplateCommand);
    }

    [Fact]
    public void DeleteTemplateCommand_ShouldExist()
    {
        var vm = new TemplatesViewModel();
        Assert.NotNull(vm.DeleteTemplateCommand);
    }

    [Fact]
    public void RenderTemplateCommand_ShouldExist()
    {
        var vm = new TemplatesViewModel();
        Assert.NotNull(vm.RenderTemplateCommand);
    }
}
