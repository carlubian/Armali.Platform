FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props Blackwing.slnx ./
COPY src/backend ./src/backend
RUN dotnet restore src/backend/Blackwing.Api/Blackwing.Api.csproj
RUN dotnet publish src/backend/Blackwing.Api/Blackwing.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Blackwing.Api.dll"]
