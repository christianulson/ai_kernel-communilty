using System.Text.Json.Serialization;

namespace KrnlAI.Sdk.Models;

public sealed record HealthStatus(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("ts")] string Ts
);
