FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY ["backend/backend.csproj", "./"]
RUN dotnet restore
COPY backend/. .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENTRYPOINT ["dotnet", "backend.dll"] 