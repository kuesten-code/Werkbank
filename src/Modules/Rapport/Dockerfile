# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Kuestencode.Shared.Contracts/Kuestencode.Shared.Contracts.csproj", "src/Kuestencode.Shared.Contracts/"]
COPY ["src/Kuestencode.Shared.ApiClients/Kuestencode.Shared.ApiClients.csproj", "src/Kuestencode.Shared.ApiClients/"]
COPY ["src/Modules/Rapport/Kuestencode.Rapport.csproj", "src/Modules/Rapport/"]

# Restore dependencies
RUN dotnet restore "src/Modules/Rapport/Kuestencode.Rapport.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "src/Modules/Rapport/Kuestencode.Rapport.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Modules/Rapport/Kuestencode.Rapport.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8082

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Kuestencode.Rapport.dll"]
