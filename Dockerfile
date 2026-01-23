# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Kuestencode.Werkbank.sln", "./"]
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Host/Kuestencode.Werkbank.Host.csproj", "src/Host/"]
COPY ["src/Modules/Faktura/Kuestencode.Faktura.csproj", "src/Modules/Faktura/"]

# Restore dependencies (Host will restore all dependencies including Faktura)
RUN dotnet restore "src/Host/Kuestencode.Werkbank.Host.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "src/Host/Kuestencode.Werkbank.Host.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Host/Kuestencode.Werkbank.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Create directory for data protection keys
RUN mkdir -p /app/data/keys

ENTRYPOINT ["dotnet", "Kuestencode.Werkbank.Host.dll"]
