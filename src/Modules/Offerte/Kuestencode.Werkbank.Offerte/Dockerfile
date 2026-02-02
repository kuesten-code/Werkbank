# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Kuestencode.Shared.Contracts/Kuestencode.Shared.Contracts.csproj", "src/Kuestencode.Shared.Contracts/"]
COPY ["src/Kuestencode.Shared.ApiClients/Kuestencode.Shared.ApiClients.csproj", "src/Kuestencode.Shared.ApiClients/"]
COPY ["src/Modules/Offerte/Kuestencode.Werkbank.Offerte.Domain/Kuestencode.Werkbank.Offerte.Domain.csproj", "src/Modules/Offerte/Kuestencode.Werkbank.Offerte.Domain/"]
COPY ["src/Modules/Offerte/Kuestencode.Werkbank.Offerte.Data/Kuestencode.Werkbank.Offerte.Data.csproj", "src/Modules/Offerte/Kuestencode.Werkbank.Offerte.Data/"]
COPY ["src/Modules/Offerte/Kuestencode.Werkbank.Offerte/Kuestencode.Werkbank.Offerte.csproj", "src/Modules/Offerte/Kuestencode.Werkbank.Offerte/"]

# Restore dependencies
RUN dotnet restore "src/Modules/Offerte/Kuestencode.Werkbank.Offerte/Kuestencode.Werkbank.Offerte.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "src/Modules/Offerte/Kuestencode.Werkbank.Offerte/Kuestencode.Werkbank.Offerte.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Modules/Offerte/Kuestencode.Werkbank.Offerte/Kuestencode.Werkbank.Offerte.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8083

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Kuestencode.Werkbank.Offerte.dll"]
