# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ElasticPerformance.sln", "./"]
COPY ["src/ElasticPerformance.API/ElasticPerformance.API.csproj", "src/ElasticPerformance.API/"]
COPY ["src/ElasticPerformance.Application/ElasticPerformance.Application.csproj", "src/ElasticPerformance.Application/"]
COPY ["src/ElasticPerformance.Domain/ElasticPerformance.Domain.csproj", "src/ElasticPerformance.Domain/"]
COPY ["src/ElasticPerformance.Infrastructure/ElasticPerformance.Infrastructure.csproj", "src/ElasticPerformance.Infrastructure/"]

RUN dotnet restore "src/ElasticPerformance.API/ElasticPerformance.API.csproj"

COPY . .

WORKDIR "/src/src/ElasticPerformance.API"
RUN dotnet publish "ElasticPerformance.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ElasticPerformance.API.dll"]
