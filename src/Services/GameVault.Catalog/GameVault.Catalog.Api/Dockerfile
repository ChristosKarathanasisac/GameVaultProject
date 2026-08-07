FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer
COPY ["GameVault.Catalog.Api/GameVault.Catalog.Api/GameVault.Catalog.Api.csproj", "GameVault.Catalog.Api/GameVault.Catalog.Api/"]
COPY ["GameVault.Catalog.Infrastructure/GameVault.Catalog.Infrastructure.csproj", "GameVault.Catalog.Infrastructure/"]
COPY ["GameVault.Catalog.Application/GameVault.Catalog.Application.csproj", "GameVault.Catalog.Application/"]
COPY ["GameVault.Catalog.Domain/GameVault.Catalog.Domain.csproj", "GameVault.Catalog.Domain/"]
RUN dotnet restore "GameVault.Catalog.Api/GameVault.Catalog.Api/GameVault.Catalog.Api.csproj"

# Copy the rest of the source and build
COPY . .
WORKDIR "/src/GameVault.Catalog.Api/GameVault.Catalog.Api"
RUN dotnet build "GameVault.Catalog.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameVault.Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameVault.Catalog.Api.dll"]
