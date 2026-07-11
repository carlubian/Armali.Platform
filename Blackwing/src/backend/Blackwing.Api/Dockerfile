FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props Blackwing.slnx ./
COPY src/backend ./src/backend
RUN dotnet restore src/backend/Blackwing.Api/Blackwing.Api.csproj
RUN dotnet publish src/backend/Blackwing.Api/Blackwing.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Magick.NET's bundled native ImageMagick needs libgomp1 at runtime.
# wget backs the container healthcheck; the aspnet base image ships neither wget nor curl.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 wget \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Blackwing.Api.dll"]
