FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer
COPY ["Services/GameVault.Notifications/GameVault.Notifications.Api/GameVault.Notifications.Api/GameVault.Notifications.Api.csproj", "Services/GameVault.Notifications/GameVault.Notifications.Api/GameVault.Notifications.Api/"]
COPY ["Services/GameVault.Notifications/GameVault.Notifications.Infrastructure/GameVault.Notifications.Infrastructure.csproj", "Services/GameVault.Notifications/GameVault.Notifications.Infrastructure/"]
COPY ["Services/GameVault.Notifications/GameVault.Notifications.Application/GameVault.Notifications.Application.csproj", "Services/GameVault.Notifications/GameVault.Notifications.Application/"]
COPY ["Services/GameVault.Notifications/GameVault.Notifications.Domain/GameVault.Notifications.Domain.csproj", "Services/GameVault.Notifications/GameVault.Notifications.Domain/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Contracts/GameVault.Contracts.csproj", "SharedLibraries/GameVault.Common/GameVault.Contracts/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Core/GameVault.Core.csproj", "SharedLibraries/GameVault.Common/GameVault.Core/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.SharedKernel/GameVault.SharedKernel.csproj", "SharedLibraries/GameVault.Common/GameVault.SharedKernel/"]
COPY ["NuGet.Config", "."]
RUN --mount=type=cache,id=nuget-notifications,target=/root/.nuget/packages \
    dotnet restore "Services/GameVault.Notifications/GameVault.Notifications.Api/GameVault.Notifications.Api/GameVault.Notifications.Api.csproj"

# Copy the rest of the source and build
COPY . .
WORKDIR "/src/Services/GameVault.Notifications/GameVault.Notifications.Api/GameVault.Notifications.Api"
RUN --mount=type=cache,id=nuget-notifications,target=/root/.nuget/packages \
    dotnet build "GameVault.Notifications.Api.csproj" -c Debug -o /app/build

FROM build AS publish
RUN --mount=type=cache,id=nuget-notifications,target=/root/.nuget/packages \
    dotnet publish "GameVault.Notifications.Api.csproj" -c Debug -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameVault.Notifications.Api.dll"]
