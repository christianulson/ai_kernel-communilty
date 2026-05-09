namespace Kernel.Contracts;

public sealed record PolicySignal(
    string SignalType,
    string Domain,
    string Description,
    double Confidence
);
