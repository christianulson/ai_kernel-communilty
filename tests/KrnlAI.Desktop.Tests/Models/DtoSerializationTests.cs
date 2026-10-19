using System.Text.Json;

namespace KrnlAI.Desktop.Tests.Models;

public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void LoginRequest_SerializesAsEmail()
    {
        var req = new LoginRequest("admin@test.com", "pass123");
        var json = JsonSerializer.Serialize(req, JsonOptions);
        Assert.Contains("\"email\"", json);
        Assert.DoesNotContain("\"username\"", json);
    }

    [Fact]
    public void LoginResponseDto_CanDeserializeFromApiResponse()
    {
        var apiJson = """
        {
            "token": "eyJhbGciOiJIUzI1NiJ9.eyJ1c2VyIjoiYWRtaW4ifQ",
            "refreshToken": "rt_abc123",
            "user": {
                "id": "admin-001",
                "email": "admin@ai-kernel.local",
                "name": "Administrator",
                "roles": ["admin"]
            }
        }
        """;
        var dto = JsonSerializer.Deserialize<LoginResponseDto>(apiJson, JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("eyJhbGciOiJIUzI1NiJ9.eyJ1c2VyIjoiYWRtaW4ifQ", dto.Token);
        Assert.Equal("rt_abc123", dto.RefreshToken);
        Assert.NotNull(dto.User);
        Assert.Equal("admin-001", dto.User.Id);
        Assert.Equal("admin@ai-kernel.local", dto.User.Email);
        Assert.Equal("Administrator", dto.User.Name);
        Assert.Contains("admin", dto.User.Roles);
    }
}
