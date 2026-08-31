# ==============================================================================
# Stage 1: Base Runtime Image
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 8080

# Configure default ASP.NET Core URL bindings
ENV ASPNETCORE_URLS=http://+:5000;http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# ==============================================================================
# Stage 2: SDK Build & Restore
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (cached layer)
COPY ["TrackerKerja.csproj", "./"]
RUN dotnet restore "TrackerKerja.csproj"

# Copy the rest of the application source code
COPY . .
WORKDIR "/src"
RUN dotnet build "TrackerKerja.csproj" -c Release -o /app/build

# ==============================================================================
# Stage 3: Publish App
# ==============================================================================
FROM build AS publish
RUN dotnet publish "TrackerKerja.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 4: Final Container Image
# ==============================================================================
FROM base AS final
WORKDIR /app

# Copy compiled binaries and assets from publish stage
COPY --from=publish /app/publish .

# Pre-create persistent data directories
RUN mkdir -p /app/data \
    && mkdir -p /app/wwwroot/uploads/notes \
    && mkdir -p /app/wwwroot/uploads/avatars

# Volume mounts for persistent SQLite database and user file uploads
VOLUME ["/app/data", "/app/wwwroot/uploads"]

ENTRYPOINT ["dotnet", "TrackerKerja.dll"]
