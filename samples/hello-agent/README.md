# Hello Agent Sample

This sample shows the smallest local workflow:

```bash
dotnet tool install -g AIKernel.Cli
aikernel chat --local
```

Try:

```text
Remember that this project uses local-first memory.
What do you remember about this project?
```

The first prompt writes memory through the embedded kernel. The second prompt
retrieves it through the local runtime.
