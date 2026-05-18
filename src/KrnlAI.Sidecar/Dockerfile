FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/KrnlAI.Sidecar/KrnlAI.Sidecar.csproj", "src/KrnlAI.Sidecar/"]
COPY ["src/Kernel.Core/Kernel.Core.csproj", "src/Kernel.Core/"]
COPY ["src/Kernel.Contracts/Kernel.Contracts.csproj", "src/Kernel.Contracts/"]
COPY ["src/Kernel.Infrastructure/Kernel.Infrastructure.csproj", "src/Kernel.Infrastructure/"]
RUN dotnet restore "src/KrnlAI.Sidecar/KrnlAI.Sidecar.csproj"
COPY . .
WORKDIR "/src/src/KrnlAI.Sidecar"
RUN dotnet build "KrnlAI.Sidecar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KrnlAI.Sidecar.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KrnlAI.Sidecar.dll"]
