FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Directory.Build.Diagnostics.props .
COPY Directory.Build.Mcp.props .
COPY Directory.Packages.props .
COPY LuminaUI.slnx .
COPY tools/LuminaUI.Mcp/ tools/LuminaUI.Mcp/

RUN dotnet publish tools/LuminaUI.Mcp/LuminaUI.Mcp.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends git curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/data
VOLUME /app/data

ENV Catalog__DatabasePath=/app/data/lumina-mcp.db
ENV Catalog__RemoteUrl=https://github.com/j4587698/LuminaUI.git
ENV Catalog__Port=3001
ENV Catalog__AutoIndexOnEmpty=false
ENV Catalog__DefaultRef=
ENV Admin__Username=admin
ENV Admin__RequireConfiguredPassword=true
EXPOSE 3001

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=5 CMD curl -f "http://localhost:${Catalog__Port:-3001}/readyz" || exit 1

ENTRYPOINT ["dotnet", "LuminaUI.Mcp.dll"]
