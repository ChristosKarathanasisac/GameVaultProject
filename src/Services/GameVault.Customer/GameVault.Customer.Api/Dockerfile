FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer
COPY ["Services/GameVault.Customer/GameVault.Customer.Api/GameVault.Customer.Api/GameVault.Customer.Api.csproj", "Services/GameVault.Customer/GameVault.Customer.Api/GameVault.Customer.Api/"]
COPY ["Services/GameVault.Customer/GameVault.Customer.Infrastructure/GameVault.Customer.Infrastructure.csproj", "Services/GameVault.Customer/GameVault.Customer.Infrastructure/"]
COPY ["Services/GameVault.Customer/GameVault.Customer.Application/GameVault.Customer.Application.csproj", "Services/GameVault.Customer/GameVault.Customer.Application/"]
COPY ["Services/GameVault.Customer/GameVault.Customer.Domain/GameVault.Customer.Domain.csproj", "Services/GameVault.Customer/GameVault.Customer.Domain/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Common/GameVault.Common.csproj", "SharedLibraries/GameVault.Common/GameVault.Common/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Contracts/GameVault.Contracts.csproj", "SharedLibraries/GameVault.Common/GameVault.Contracts/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.Core/GameVault.Core.csproj", "SharedLibraries/GameVault.Common/GameVault.Core/"]
COPY ["SharedLibraries/GameVault.Common/GameVault.SharedKernel/GameVault.SharedKernel.csproj", "SharedLibraries/GameVault.Common/GameVault.SharedKernel/"]
RUN dotnet restore "Services/GameVault.Customer/GameVault.Customer.Api/GameVault.Customer.Api/GameVault.Customer.Api.csproj"

# Copy the rest of the source and build
COPY . .
WORKDIR "/src/Services/GameVault.Customer/GameVault.Customer.Api/GameVault.Customer.Api"
RUN dotnet build "GameVault.Customer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameVault.Customer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameVault.Customer.Api.dll"]
