# syntax=docker/dockerfile:1

# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy source (use .dockerignore to exclude bin/obj/.git etc.)
COPY . .

# Restore & publish only the web server project (transitively restores referenced modules)
WORKDIR /src/src/Presentation.Web.Server
RUN dotnet restore Presentation.Web.Server.csproj \
 && dotnet publish Presentation.Web.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:8080 \
 ASPNETCORE_ENVIRONMENT=LocalDevelopment

# Expose HTTP port
EXPOSE 8080

# TODO: Add a health endpoint in app and then enable a healthcheck here, e.g.:
# HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
# CMD wget -qO- http://localhost:8080/health || exit1

ENTRYPOINT ["dotnet", "BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server.dll"]
