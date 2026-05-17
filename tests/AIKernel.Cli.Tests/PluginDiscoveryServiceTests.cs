using System.Net;
using AIKernel.Cli.Services;

namespace AIKernel.Cli.Tests;

public sealed class PluginDiscoveryServiceTests
{
    [Fact]
    public async Task PluginDiscoveryService_RegistryUnreachable_ShouldReturnEmptySearch()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"version":1,"plugins":[]}""");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:1/") };
        var service = new PluginDiscoveryService(httpClient);

        var results = await service.SearchAsync("filesystem");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task PluginDiscoveryService_Search_ShouldFindMatchingPlugins()
    {
        var json = """
        {
          "version": 1,
          "plugins": [
            {
              "id": "mcp-filesystem",
              "name": "MCP Filesystem",
              "description": "Access local filesystem via MCP",
              "author": "aikernel",
              "type": "mcp",
              "url": "https://github.com/aikernel/mcp-filesystem",
              "risk": "low",
              "verified": true,
              "version": "1.0.0"
            },
            {
              "id": "web-search",
              "name": "Web Search",
              "description": "Web search and content extraction",
              "author": "community",
              "type": "mcp",
              "url": "https://github.com/community/web-search-mcp",
              "risk": "medium",
              "verified": false,
              "version": "1.2.0"
            }
          ]
        }
        """;
        var handler = new MockHttpHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:2/") };
        var service = new PluginDiscoveryService(httpClient);

        var results = await service.SearchAsync("filesystem");

        results.Should().HaveCount(1);
        results[0].Id.Should().Be("mcp-filesystem");
    }

    [Fact]
    public async Task PluginDiscoveryService_GetById_ShouldReturnMatchingPlugin()
    {
        var json = """
        {
          "version": 1,
          "plugins": [
            {
              "id": "github-tools",
              "name": "GitHub Tools",
              "description": "GitHub API integration",
              "author": "aikernel",
              "type": "openapi",
              "url": "https://github.com/aikernel/github-tools",
              "risk": "low",
              "verified": true,
              "version": "0.9.0"
            }
          ]
        }
        """;
        var handler = new MockHttpHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3/") };
        var service = new PluginDiscoveryService(httpClient);

        var plugin = await service.GetByIdAsync("github-tools");

        plugin.Should().NotBeNull();
        plugin!.Name.Should().Be("GitHub Tools");
    }

    [Fact]
    public async Task PluginDiscoveryService_GetById_UnknownPlugin_ShouldReturnNull()
    {
        var json = """
        {
          "version": 1,
          "plugins": []
        }
        """;
        var handler = new MockHttpHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:4/") };
        var service = new PluginDiscoveryService(httpClient);

        var plugin = await service.GetByIdAsync("nonexistent");

        plugin.Should().BeNull();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
