FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY StockAnalyzer.Api.csproj ./
COPY StockAnalyzer.Application/StockAnalyzer.Application.csproj StockAnalyzer.Application/
COPY StockAnalyzer.Domain/StockAnalyzer.Domain.csproj StockAnalyzer.Domain/
COPY StockAnalyzer.Infrastructure/StockAnalyzer.Infrastructure.csproj StockAnalyzer.Infrastructure/
RUN dotnet restore StockAnalyzer.Api.csproj
COPY . .
RUN dotnet publish StockAnalyzer.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StockAnalyzer.Api.dll"]
