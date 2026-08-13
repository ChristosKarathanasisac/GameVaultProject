FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Gateway/GameVault.WebEdge.Api/GameVault.WebEdge.Api.csproj", "Gateway/GameVault.WebEdge.Api/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Core/GameVault.Core.csproj", "SharedLibraries/GameVault.Common/GameVault.Core/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.SharedKernel/GameVault.SharedKernel.csproj", "SharedLibraries/GameVault.Common/GameVault.SharedKernel/"]
COPY ["NuGet.Config", "."]
RUN --mount=type=cache,id=nuget-webedge,target=/root/.nuget/packages \
    dotnet restore "Gateway/GameVault.WebEdge.Api/GameVault.WebEdge.Api.csproj"

COPY . .
WORKDIR "/src/Gateway/GameVault.WebEdge.Api"
RUN --mount=type=cache,id=nuget-webedge,target=/root/.nuget/packages \
    dotnet build "GameVault.WebEdge.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,id=nuget-webedge,target=/root/.nuget/packages \
    dotnet publish "GameVault.WebEdge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameVault.WebEdge.Api.dll"]
