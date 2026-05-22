using KrnlAI.Sample.CustomTool;

var tool = new TodoTool();
Console.WriteLine($"Tool: {tool.Name} — {tool.Description}");
Console.WriteLine();

var input = new TodoInput("Buy milk");
var result = await tool.ExecuteAsync(input);
Console.WriteLine($"Created: [{result.Id}] {result.Title} (done: {result.IsComplete})");

var input2 = new TodoInput("Write samples");
var result2 = await tool.ExecuteAsync(input2);
Console.WriteLine($"Created: [{result2.Id}] {result2.Title} (done: {result2.IsComplete})");

Console.WriteLine();
Console.WriteLine($"Total items: {tool.List().Count}");
foreach (var item in tool.List())
    Console.WriteLine($"  - [{item.Id}] {item.Title}");
