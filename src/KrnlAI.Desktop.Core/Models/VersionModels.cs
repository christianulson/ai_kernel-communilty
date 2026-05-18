namespace KrnlAI.Desktop.Core.Models;

public record VersionsInfo(string DefaultVersion, IReadOnlyList<string> SupportedVersions, bool LegacyUnversionedDeprecated, string LegacySunsetDate);

public record ContractEntry(string Endpoint, string ContractVersion, string SupportedRange, bool Deprecated, string State);

public record ContractsResponse(string DefaultApiVersion, IReadOnlyList<ContractEntry> Contracts);
