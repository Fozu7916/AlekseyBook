FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Установка сертификатов SSL
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["backend.csproj", "."]
RUN dotnet restore "./backend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Переменные окружения для MySQL
ENV RAILWAY_TCP_PROXY_DOMAIN=containers-us-west-207.railway.app
ENV RAILWAY_TCP_PROXY_PORT=6974
ENV MYSQL_DATABASE=railway
ENV MYSQLUSER=root
ENV MYSQL_ROOT_PASSWORD=VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww

ENTRYPOINT ["dotnet", "backend.dll"] 