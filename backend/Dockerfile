FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
ENV MYSQL_HOST=roundhouse.proxy.rlwy.net
ENV MYSQL_PORT=33306
ENV MYSQL_DATABASE=railway
ENV MYSQL_USER=root
ENV MYSQL_PASSWORD=VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww

EXPOSE 8080

ENTRYPOINT ["dotnet", "backend.dll"] 