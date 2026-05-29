# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (order matters for effective layer caching)
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Kuestencode.Shared.Contracts/Kuestencode.Shared.Contracts.csproj", "src/Kuestencode.Shared.Contracts/"]
COPY ["src/Kuestencode.Shared.ApiClients/Kuestencode.Shared.ApiClients.csproj", "src/Kuestencode.Shared.ApiClients/"]
COPY ["src/Modules/Contracta/Kuestencode.Werkbank.Contracta.Domain/Kuestencode.Werkbank.Contracta.Domain.csproj", "src/Modules/Contracta/Kuestencode.Werkbank.Contracta.Domain/"]
COPY ["src/Modules/Contracta/Kuestencode.Werkbank.Contracta.Data/Kuestencode.Werkbank.Contracta.Data.csproj", "src/Modules/Contracta/Kuestencode.Werkbank.Contracta.Data/"]
COPY ["src/Modules/Contracta/Kuestencode.Werkbank.Contracta/Kuestencode.Werkbank.Contracta.csproj", "src/Modules/Contracta/Kuestencode.Werkbank.Contracta/"]

# Restore dependencies
RUN dotnet restore "src/Modules/Contracta/Kuestencode.Werkbank.Contracta/Kuestencode.Werkbank.Contracta.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "src/Modules/Contracta/Kuestencode.Werkbank.Contracta/Kuestencode.Werkbank.Contracta.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Modules/Contracta/Kuestencode.Werkbank.Contracta/Kuestencode.Werkbank.Contracta.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8087

RUN apt-get update && \
    apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
ENV TZ=Europe/Berlin

ARG DOCKER_IMAGE_TAG=dev
ENV DOCKER_IMAGE_TAG=${DOCKER_IMAGE_TAG}

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Kuestencode.Werkbank.Contracta.dll"]
