# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (order matters for effective layer caching)
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Kuestencode.Shared.Contracts/Kuestencode.Shared.Contracts.csproj", "src/Kuestencode.Shared.Contracts/"]
COPY ["src/Kuestencode.Shared.ApiClients/Kuestencode.Shared.ApiClients.csproj", "src/Kuestencode.Shared.ApiClients/"]
COPY ["src/Modules/Acta/Kuestencode.Werkbank.Acta.Domain/Kuestencode.Werkbank.Acta.Domain.csproj", "src/Modules/Acta/Kuestencode.Werkbank.Acta.Domain/"]
COPY ["src/Modules/Acta/Kuestencode.Werkbank.Acta.Data/Kuestencode.Werkbank.Acta.Data.csproj", "src/Modules/Acta/Kuestencode.Werkbank.Acta.Data/"]
COPY ["src/Modules/Acta/Kuestencode.Werkbank.Acta/Kuestencode.Werkbank.Acta.csproj", "src/Modules/Acta/Kuestencode.Werkbank.Acta/"]

# Restore dependencies
RUN dotnet restore "src/Modules/Acta/Kuestencode.Werkbank.Acta/Kuestencode.Werkbank.Acta.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "src/Modules/Acta/Kuestencode.Werkbank.Acta/Kuestencode.Werkbank.Acta.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Modules/Acta/Kuestencode.Werkbank.Acta/Kuestencode.Werkbank.Acta.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8084

# Surface build metadata for observability
ARG DOCKER_IMAGE_TAG=dev
ENV DOCKER_IMAGE_TAG=${DOCKER_IMAGE_TAG}

# Copy published output
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Kuestencode.Werkbank.Acta.dll"]
