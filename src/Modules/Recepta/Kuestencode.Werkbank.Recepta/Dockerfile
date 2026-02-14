# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (order matters for effective layer caching)
COPY ["src/Core/Kuestencode.Core.csproj", "src/Core/"]
COPY ["src/Shared.UI/Kuestencode.Shared.UI.csproj", "src/Shared.UI/"]
COPY ["src/Kuestencode.Shared.Contracts/Kuestencode.Shared.Contracts.csproj", "src/Kuestencode.Shared.Contracts/"]
COPY ["src/Kuestencode.Shared.ApiClients/Kuestencode.Shared.ApiClients.csproj", "src/Kuestencode.Shared.ApiClients/"]
COPY ["src/Modules/Recepta/Kuestencode.Werkbank.Recepta.Domain/Kuestencode.Werkbank.Recepta.Domain.csproj", "src/Modules/Recepta/Kuestencode.Werkbank.Recepta.Domain/"]
COPY ["src/Modules/Recepta/Kuestencode.Werkbank.Recepta.Data/Kuestencode.Werkbank.Recepta.Data.csproj", "src/Modules/Recepta/Kuestencode.Werkbank.Recepta.Data/"]
COPY ["src/Modules/Recepta/Kuestencode.Werkbank.Recepta/Kuestencode.Werkbank.Recepta.csproj", "src/Modules/Recepta/Kuestencode.Werkbank.Recepta/"]

# Restore dependencies
RUN dotnet restore "src/Modules/Recepta/Kuestencode.Werkbank.Recepta/Kuestencode.Werkbank.Recepta.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "src/Modules/Recepta/Kuestencode.Werkbank.Recepta/Kuestencode.Werkbank.Recepta.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "src/Modules/Recepta/Kuestencode.Werkbank.Recepta/Kuestencode.Werkbank.Recepta.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8085

# Install Tesseract OCR, German language data, and poppler-utils for PDF conversion
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-deu \
        poppler-utils \
    && rm -rf /var/lib/apt/lists/*

# Surface build metadata for observability
ARG DOCKER_IMAGE_TAG=dev
ENV DOCKER_IMAGE_TAG=${DOCKER_IMAGE_TAG}

# Copy published output
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Kuestencode.Werkbank.Recepta.dll"]
