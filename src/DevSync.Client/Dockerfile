FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DevSync.Shared/DevSync.Shared.csproj", "src/DevSync.Shared/"]
COPY ["src/DevSync.Client/DevSync.Client.csproj", "src/DevSync.Client/"]
RUN dotnet restore "src/DevSync.Client/DevSync.Client.csproj"
COPY . .
RUN dotnet publish "src/DevSync.Client/DevSync.Client.csproj" -c Release -o /app/publish

FROM nginx:1.27-alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY ["src/DevSync.Client/nginx.conf", "/etc/nginx/conf.d/default.conf"]
EXPOSE 80
