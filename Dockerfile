FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/MarTech.Orders.Domain/MarTech.Orders.Domain.csproj src/MarTech.Orders.Domain/
COPY src/MarTech.Orders.Application/MarTech.Orders.Application.csproj src/MarTech.Orders.Application/
COPY src/MarTech.Orders.Infrastructure/MarTech.Orders.Infrastructure.csproj src/MarTech.Orders.Infrastructure/
COPY src/MarTech.Orders.Api/MarTech.Orders.Api.csproj src/MarTech.Orders.Api/
RUN dotnet restore src/MarTech.Orders.Api/MarTech.Orders.Api.csproj

COPY src/ src/
RUN dotnet publish src/MarTech.Orders.Api/MarTech.Orders.Api.csproj \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm --recursive --force /var/lib/apt/lists/* \
    && mkdir /data \
    && chown "$APP_UID" /data

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Orders="Data Source=/data/orders.db"

EXPOSE 8080
USER $APP_UID
VOLUME ["/data"]

HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MarTech.Orders.Api.dll"]
