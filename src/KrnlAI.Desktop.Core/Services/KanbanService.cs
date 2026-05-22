using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public sealed class KanbanService(
    HttpClient http,
    ILogger<KanbanService>? logger = null)
{
    public async Task<KanbanDisplay> GetKanbanAsync(
        int daysBack = 10,
        string? domain = null,
        double? minPriority = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var url = $"/api/goals/kanban?daysBack={daysBack}";
        if (domain is not null) url += $"&domain={Uri.EscapeDataString(domain)}";
        if (minPriority.HasValue) url += $"&minPriority={minPriority.Value}";
        if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";

        logger?.LogDebug("Fetching Kanban data: {Url}", url);
        var resp = await http.GetFromJsonAsync<KanbanResponseDto>(url, ct);
        if (resp is null)
        {
            logger?.LogWarning("Kanban API returned null for: {Url}", url);
            return EmptyResponse();
        }

        return new KanbanDisplay(
            resp.Columns.Select(c => new KanbanColumnDisplay(
                c.Column, c.Label,
                c.Cards.Select(MapCard).ToList(),
                c.TotalCount)).ToList(),
            new KanbanMetadataDisplay(
                resp.Metadata.TotalGoals,
                resp.Metadata.TotalColumns,
                new KanbanFilterDisplay(
                    resp.Metadata.Filters.DaysBack,
                    resp.Metadata.Filters.Domain,
                    resp.Metadata.Filters.MinPriority,
                    resp.Metadata.Filters.UserId,
                    resp.Metadata.Filters.Search)));
    }

    public async Task<bool> MoveCardAsync(string cardId, string newStatus, CancellationToken ct = default)
    {
        logger?.LogDebug("Moving card {CardId} to {NewStatus}", cardId, newStatus);
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { status = newStatus }),
            System.Text.Encoding.UTF8,
            "application/json");
        var resp = await http.PatchAsync($"/api/goals/{cardId}/status", content, ct);
        if (!resp.IsSuccessStatusCode)
            logger?.LogWarning("Move card {CardId} failed: {Status}", cardId, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    private static KanbanCardDisplay MapCard(KanbanCardDto card) => new(
        card.Id, card.Description, card.Status, card.Progress, card.Priority,
        card.Domain, card.CreatedAt, card.Deadline, card.ParentGoalId,
        card.SubGoals?.Select(MapCard).ToList());

    private static KanbanDisplay EmptyResponse() => new(
        [],
        new KanbanMetadataDisplay(0, 0, new KanbanFilterDisplay(10, null, null, null, null)));

    private sealed record KanbanResponseDto(
        [property: JsonPropertyName("columns")] IReadOnlyList<KanbanColumnDto> Columns,
        [property: JsonPropertyName("metadata")] KanbanMetadataDto Metadata);

    private sealed record KanbanColumnDto(
        [property: JsonPropertyName("column")] string Column,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("cards")] IReadOnlyList<KanbanCardDto> Cards,
        [property: JsonPropertyName("totalCount")] int TotalCount);

    private sealed record KanbanCardDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("progress")] double Progress,
        [property: JsonPropertyName("priority")] double Priority,
        [property: JsonPropertyName("domain")] string? Domain,
        [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("deadline")] DateTimeOffset? Deadline,
        [property: JsonPropertyName("parentGoalId")] string? ParentGoalId,
        [property: JsonPropertyName("subGoals")] IReadOnlyList<KanbanCardDto>? SubGoals);

    private sealed record KanbanMetadataDto(
        [property: JsonPropertyName("totalGoals")] int TotalGoals,
        [property: JsonPropertyName("totalColumns")] int TotalColumns,
        [property: JsonPropertyName("filters")] KanbanFilterDto Filters);

    private sealed record KanbanFilterDto(
        [property: JsonPropertyName("daysBack")] int DaysBack,
        [property: JsonPropertyName("domain")] string? Domain,
        [property: JsonPropertyName("minPriority")] double? MinPriority,
        [property: JsonPropertyName("userId")] string? UserId,
        [property: JsonPropertyName("search")] string? Search);
}
