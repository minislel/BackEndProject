# 1. Etap buildowania
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy solution i csproj
COPY BackEndProject.sln ./
COPY ApplicationCore/ApplicationCore.csproj ./ApplicationCore/
COPY Infrastructure/Infrastructure.csproj ./Infrastructure/
COPY WebApi/WebApi.csproj ./WebApi/

# Przywracamy zależności
RUN dotnet restore WebApi/WebApi.csproj

# Kopiujemy całą resztę projektu
COPY ApplicationCore/ ./ApplicationCore/
COPY Infrastructure/ ./Infrastructure/
COPY WebApi/ ./WebApi/

# Buildujemy release
WORKDIR /src/WebApi
RUN dotnet publish -c Release -o /app/publish

# 2. Etap runtime (mniejszy obrazek)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Kopiujemy z poprzedniego stage'a
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
# Otwieramy port (np. 5000)
EXPOSE 5000
EXPOSE 7001
EXPOSE 5260

# Uruchamiamy aplikację
ENTRYPOINT ["dotnet", "WebApi.dll"]
