FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Belfalas.sln ./
COPY src/backend/Belfalas.Api/Belfalas.Api.csproj src/backend/Belfalas.Api/
COPY src/backend/Belfalas.Domain/Belfalas.Domain.csproj src/backend/Belfalas.Domain/
COPY src/backend/Belfalas.Persistence/Belfalas.Persistence.csproj src/backend/Belfalas.Persistence/
RUN dotnet restore src/backend/Belfalas.Api/Belfalas.Api.csproj

COPY src/backend src/backend
RUN dotnet publish src/backend/Belfalas.Api/Belfalas.Api.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN mkdir -p /data
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Belfalas.Api.dll"]
