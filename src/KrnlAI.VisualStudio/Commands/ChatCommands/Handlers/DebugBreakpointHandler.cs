using System.Text.RegularExpressions;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugBreakpointHandler
{
    private static readonly Regex FileLineRegex = new(
        @"^(.+):(\d+)$", RegexOptions.Compiled);

    public static SlashCommand Create(IVsDebugService debug) =>
        new("debug-bp", "Set a breakpoint: /debug-bp <file>:<line>",
            async (args, ct) =>
            {
                if (string.IsNullOrWhiteSpace(args))
                    return GetUsage();

                var match = FileLineRegex.Match(args.Trim());
                if (!match.Success)
                    return GetUsage();

                var filePath = match.Groups[1].Value.Trim('"');
                var line = int.Parse(match.Groups[2].Value);

                var ok = await debug.SetBreakpointAsync(filePath, line, ct);
                return ok
                    ? $"🔴 Breakpoint set at `{filePath}:{line}`."
                    : $"⚠️ Failed to set breakpoint at `{filePath}:{line}`.";
            });

    private static string GetUsage() =>
        """
### /debug-bp — Usage

Set a breakpoint at a specific file and line:

```
/debug-bp "C:\MyProject\Program.cs":42
/debug-bp Program.cs:10
```

Remove a breakpoint:

```
/debug-bp-remove <file>:<line>
```
""";
}
