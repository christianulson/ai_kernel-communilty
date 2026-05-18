using KrnlAI.Embedded;
using KrnlAI.Infrastructure.Storage.Dialect;

namespace KrnlAI.Tests.Community;

public sealed class EmbeddedKernelTests
{
    [Fact]
    public async Task EmbeddedKernel_RunAsync_ShouldReturnLocalNarrationWithoutHttp()
    {
        await using var kernel = new EmbeddedKernel(new EmbeddedKernelOptions { LLmProvider = "none" });

        var result = await kernel.RunAsync("hello community", CancellationToken.None);

        result.Narration.Should().Contain("hello community");
        result.Mode.Should().Be("community");
    }

    [Fact]
    public void SqliteDialect_GetUpsertSql_ShouldUseOnConflict()
    {
        var dialect = new SqliteDialect();

        var sql = dialect.GetUpsertSql("entities", "id,type", "id,type,json", "@id,@type,@json", "json", "@json");

        sql.Should().Contain("ON CONFLICT");
    }
}
