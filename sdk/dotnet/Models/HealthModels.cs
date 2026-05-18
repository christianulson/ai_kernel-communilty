using System.Text.Json.Serialization;

namespace KrnlAi.Sdk.Models;

public sealed record HealthStatus(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("ts")] string Ts
);
