using System.CommandLine;
using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.Cli.Services;
using Spectre.Console;

namespace KrnlAI.Cli.Commands;

public sealed class DocumentCommand(CliContext ctx, ConsoleRenderer renderer)
{
    private const string DocsBase = "/api/documents";

    public Command Build()
    {
        var cmd = new Command("document", "Manage documents")
        {
            BuildList(),
            BuildStatus(),
            BuildSearch(),
            BuildUpload()
        };
        return cmd;
    }

    private Command BuildList()
    {
        var limitOpt = new Option<int>("--limit", "Max results");
        var cmd = new Command("list", "List ingested documents") { limitOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var client = ctx.HttpClient;
            var limit = r.GetValue(limitOpt);
            var resp = await client.GetAsync($"{DocsBase}?limit={limit}", ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) { renderer.Console.MarkupLine("[red]Failed to list documents[/]"); return 1; }
            var docs = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
            if (docs.ValueKind != JsonValueKind.Array || docs.GetArrayLength() == 0)
            {
                renderer.Console.MarkupLine("[yellow]No documents found[/]");
                return 0;
            }
            var rows = new List<object>();
            foreach (var d in docs.EnumerateArray())
            {
                rows.Add(new { Id = d.GetProperty("documentId").GetString()?[..12] ?? "", File = d.GetProperty("fileName").GetString() ?? "", Format = d.GetProperty("format").GetString() ?? "", Status = d.GetProperty("status").GetString() ?? "", Chunks = d.GetProperty("chunkCount").GetInt32(), Created = d.GetProperty("createdAt").GetString()?[..16] ?? "" });
            }
            renderer.RenderTable(rows, "Id", "File", "Format", "Status", "Chunks", "Created");
            return 0;
        });
        return cmd;
    }

    private Command BuildStatus()
    {
        var idArg = new Argument<string>("id") { Description = "Document ID" };
        var cmd = new Command("status", "Show document details") { idArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var client = ctx.HttpClient;
            var id = r.GetValue(idArg)!;
            var resp = await client.GetAsync($"{DocsBase}/{Uri.EscapeDataString(id)}/status", ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) { renderer.Console.MarkupLine($"[red]Document '{id}' not found[/]"); return 1; }
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
            renderer.Console.MarkupLine($"[bold]Document ID:[/] {doc.GetProperty("documentId").GetString()}");
            renderer.Console.MarkupLine($"[bold]File:[/] {doc.GetProperty("fileName").GetString()}");
            renderer.Console.MarkupLine($"[bold]Size:[/] {doc.GetProperty("fileSize").GetInt64():N0} bytes");
            renderer.Console.MarkupLine($"[bold]Format:[/] {doc.GetProperty("format").GetString()}");
            renderer.Console.MarkupLine($"[bold]Status:[/] {doc.GetProperty("status").GetString()}");
            var err = doc.TryGetProperty("errorMessage", out var errEl) && errEl.ValueKind == JsonValueKind.String ? errEl.GetString() : null;
            renderer.Console.MarkupLine($"[bold]Error:[/] {err ?? "none"}");
            renderer.Console.MarkupLine($"[bold]Chunks:[/] {doc.GetProperty("chunkCount").GetInt32()}");
            renderer.Console.MarkupLine($"[bold]Created:[/] {doc.GetProperty("createdAt").GetString()}");
            if (doc.TryGetProperty("completedAt", out var cmpEl) && cmpEl.ValueKind == JsonValueKind.String)
                renderer.Console.MarkupLine($"[bold]Completed:[/] {cmpEl.GetString()}");
            return 0;
        });
        return cmd;
    }

    private Command BuildSearch()
    {
        var queryArg = new Argument<string>("query") { Description = "Search query" };
        var topKOpt = new Option<int>("--top-k", "Max results");
        var cmd = new Command("search", "Semantic search in documents") { queryArg, topKOpt };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var client = ctx.HttpClient;
            var query = r.GetValue(queryArg)!;
            var topK = r.GetValue(topKOpt);
            var body = new { query, topK };
            var resp = await client.PostAsJsonAsync($"{DocsBase}/search", body, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) { renderer.Console.MarkupLine("[red]Search failed[/]"); return 1; }
            var result = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
            if (!result.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) { renderer.Console.MarkupLine("[yellow]No results[/]"); return 0; }
            var rows = new List<object>();
            foreach (var h in hits.EnumerateArray())
                rows.Add(new { Chunk = h.GetProperty("chunkId").GetString()?[..16] ?? "", Score = $"{h.GetProperty("score").GetDouble():0.000}", Text = (h.GetProperty("text").GetString() ?? "").Length > 80 ? (h.GetProperty("text").GetString() ?? "")[..80] + "..." : h.GetProperty("text").GetString() ?? "" });
            renderer.RenderTable(rows, "Chunk", "Score", "Text Preview");
            return 0;
        });
        return cmd;
    }

    private Command BuildUpload()
    {
        var fileArg = new Argument<FileInfo>("file") { Description = "File path to upload" };
        var cmd = new Command("upload", "Upload a document") { fileArg };
        cmd.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var client = ctx.HttpClient;
            var file = r.GetValue(fileArg)!;
            if (!file.Exists) { renderer.Console.MarkupLine($"[red]File not found: {file.FullName}[/]"); return 1; }
            using var stream = file.OpenRead();
            using var content = new MultipartFormDataContent { { new StreamContent(stream), "file", file.Name } };
            renderer.Console.MarkupLine($"[yellow]Uploading {file.Name}...[/]");
            var resp = await client.PostAsync("/api/documents/upload", content, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) { renderer.Console.MarkupLine($"[red]Upload failed (HTTP {(int)resp.StatusCode})[/]"); return 1; }
            var result = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
            renderer.Console.MarkupLine($"[green]Uploaded![/] ID: {result.GetProperty("documentId").GetString()} Status: {result.GetProperty("status").GetString()}");
            return 0;
        });
        return cmd;
    }
}
