# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Kuestencode.sln", "./"]
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Modules/Faktura/Kuestencode.Faktura.csproj", "src/Modules/Faktura/"]

# Restore dependencies
RUN dotnet restore "src/Core/Kuestencode.Core.csproj"
RUN dotnet restore "src/Shared.UI/Kuestencode.Shared.UI.csproj"
RUN dotnet restore "src/Modules/Faktura/Kuestencode.Faktura.csproj"

# Copy all source code
COPY src/ src/

# Build the Faktura module
WORKDIR /src/src/Modules/Faktura
RUN dotnet build "Kuestencode.Faktura.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Kuestencode.Faktura.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Create directory for data protection keys
RUN mkdir -p /app/data/keys

ENTRYPOINT ["dotnet", "Kuestencode.Faktura.dll"]
