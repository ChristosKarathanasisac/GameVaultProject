FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer
COPY ["Services/GameVault.Catalog/GameVault.Catalog.Api/GameVault.Catalog.Api/GameVault.Catalog.Api.csproj", "Services/GameVault.Catalog/GameVault.Catalog.Api/GameVault.Catalog.Api/"]
COPY ["Services/GameVault.Catalog/GameVault.Catalog.Infrastructure/GameVault.Catalog.Infrastructure.csproj", "Services/GameVault.Catalog/GameVault.Catalog.Infrastructure/"]
COPY ["Services/GameVault.Catalog/GameVault.Catalog.Application/GameVault.Catalog.Application.csproj", "Services/GameVault.Catalog/GameVault.Catalog.Application/"]
COPY ["Services/GameVault.Catalog/GameVault.Catalog.Domain/GameVault.Catalog.Domain.csproj", "Services/GameVault.Catalog/GameVault.Catalog.Domain/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Contracts/GameVault.Contracts.csproj", "SharedLibraries/GameVault.Common/GameVault.Contracts/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Core/GameVault.Core.csproj", "SharedLibraries/GameVault.Common/GameVault.Core/"]
COPY ["NuGet.Config", "."]
RUN --mount=type=cache,id=nuget-catalog,target=/root/.nuget/packages \
    dotnet restore "Services/GameVault.Catalog/GameVault.Catalog.Api/GameVault.Catalog.Api/GameVault.Catalog.Api.csproj"

# Copy the rest of the source and build
COPY . .
WORKDIR "/src/Services/GameVault.Catalog/GameVault.Catalog.Api/GameVault.Catalog.Api"
RUN dotnet build "GameVault.Catalog.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameVault.Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameVault.Catalog.Api.dll"]
