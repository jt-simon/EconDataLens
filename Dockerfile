# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching on restore
COPY EconDataLens.sln global.json Directory.Build.props ./
COPY EconDataLens.Domain/EconDataLens.Domain.csproj EconDataLens.Domain/
COPY EconDataLens.Application/EconDataLens.Application.csproj EconDataLens.Application/
COPY EconDataLens.Infrastructure/EconDataLens.Infrastructure.csproj EconDataLens.Infrastructure/
COPY EconDataLens.Etl/EconDataLens.Etl.csproj EconDataLens.Etl/
COPY EconDataLens.Tests/EconDataLens.Tests.csproj EconDataLens.Tests/
RUN dotnet restore EconDataLens.sln

# Copy everything else and publish
COPY . .
RUN dotnet publish EconDataLens.Etl/EconDataLens.Etl.csproj -c Release -o /publish

# Final stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
# libgssapi-krb5-2 is optional for Npgsql's Kerberos probe; without it Npgsql logs
# "Cannot load library libgssapi_krb5.so.2" on every connection.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /publish .
# Baked-in default settings; the connection string inside is always overridden
# by the ConnectionStrings__Postgres environment variable.
COPY EconDataLens.Etl/appsettings.example.json appsettings.json

# Run unprivileged. The downloads directory is created and owned here so the named
# volume mounted over it (see docker-compose.yml) is writable by the app user.
RUN mkdir -p /app/downloads && chown app:app /app/downloads
USER app

ENTRYPOINT ["dotnet", "EconDataLens.Etl.dll"]
