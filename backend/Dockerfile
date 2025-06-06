FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
ENV MYSQL_DATABASE=railway
ENV MYSQLUSER=root
ENV MYSQL_ROOT_PASSWORD=VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww
ENV RAILWAY_TCP_PROXY_DOMAIN=containers-us-west-207.railway.app
ENV RAILWAY_TCP_PROXY_PORT=6974

EXPOSE 8080

ENTRYPOINT ["dotnet", "backend.dll"] 