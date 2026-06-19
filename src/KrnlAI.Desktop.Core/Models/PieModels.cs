namespace KrnlAI.Desktop.Core.Models;

public sealed record PieInferRequest(string Premise, string? Context);

public sealed record PieInferResponse(string Conclusion, double Confidence, List<string>? SupportingEvidence);

public sealed record PieChainRequest(string InitialPremise, int Steps, string? Context);

public sealed record PieChainStep(int Step, string Premise, string Conclusion, double Confidence);

public sealed record PieChainResponse(List<PieChainStep> Steps);

public sealed record PieKnowledgeRequest(string Domain, string Fact, double Certainty);

public sealed record PieKnowledgeResponse(bool Success);

public sealed record PieCoherenceData(double OverallCoherence, List<PieCoherenceEntry> Entries);

public sealed record PieCoherenceEntry(string Id, string Statement, double CoherenceScore);

public sealed record PieTerm(string Id, string Name, string? Description, int OccurrenceCount);
