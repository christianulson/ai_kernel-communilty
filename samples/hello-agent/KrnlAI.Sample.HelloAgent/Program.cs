using KrnlAI.Sample.HelloAgent;

Console.WriteLine("Krnl-AI Hello Agent Sample");
Console.WriteLine();

var kernelUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
var goal = args.Length > 1 ? args[1] : "Say hello and introduce yourself";

Console.WriteLine($"Kernel URL: {kernelUrl}");
Console.WriteLine($"Goal:       {goal}");
Console.WriteLine();

var client = new KernelClient(new HttpClient { BaseAddress = new Uri(kernelUrl) });

try
{
    var result = await client.RunAgentAsync(goal);
    Console.WriteLine("=== Agent Response ===");
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Summary: {result.Summary}");
    Console.WriteLine($"Steps executed: {result.Steps?.Length ?? 0}");
    Console.WriteLine();
    Console.WriteLine("Sample completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Could not connect to kernel at {kernelUrl}.");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Make sure a Krnl-AI kernel is running on the expected URL.");
    Console.WriteLine("To start one: dotnet run --project ../../src/KrnlAI.Api");
}
