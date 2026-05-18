FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DevSync.Shared/DevSync.Shared.csproj", "src/DevSync.Shared/"]
COPY ["src/DevSync.Api/DevSync.Api.csproj", "src/DevSync.Api/"]
RUN dotnet restore "src/DevSync.Api/DevSync.Api.csproj"
COPY . .
RUN dotnet publish "src/DevSync.Api/DevSync.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DevSync.Api.dll"]
