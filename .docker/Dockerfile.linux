ARG APP_VERSION="1.0.0"

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION="1.0.0"
WORKDIR /src

COPY Vectra.slnx .

COPY src/Vectra/Vectra.csproj src/Vectra/
COPY src/Vectra.Application/Vectra.Application.csproj src/Vectra.Application/
COPY src/Vectra.Domain/Vectra.Domain.csproj src/Vectra.Domain/
COPY src/Vectra.BuildingBlocks/Vectra.BuildingBlocks.csproj src/Vectra.BuildingBlocks/
COPY src/Vectra.Infrastructure/Vectra.Infrastructure.csproj src/Vectra.Infrastructure/
COPY src/Vectra.Infrastructure.Persistence.Common/Vectra.Infrastructure.Persistence.Common.csproj src/Vectra.Infrastructure.Persistence.Common/
COPY src/Vectra.Infrastructure.Persistence.Sqlite/Vectra.Infrastructure.Persistence.Sqlite.csproj src/Vectra.Infrastructure.Persistence.Sqlite/

RUN dotnet restore src/Vectra/Vectra.csproj --runtime linux-x64

COPY src/ src/

RUN dotnet publish "./src/Vectra/Vectra.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=${APP_VERSION} \
    --runtime "linux-x64"

# Runtime stage (IMPORTANT: match glibc runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:7080
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 7080

ENTRYPOINT ["dotnet", "vectra.dll"]