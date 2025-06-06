FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
ENV MYSQL_URL="mysql://root:VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww@roundhouse.proxy.rlwy.net:33306/railway"

EXPOSE 8080

ENTRYPOINT ["dotnet", "backend.dll"] 