FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app/backend
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/backend/out .
ENTRYPOINT ["dotnet", "backend.dll"] 