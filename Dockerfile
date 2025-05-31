FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app
COPY . .

# Переходим в директорию бэкенда для сборки
WORKDIR /app/backend
RUN dotnet restore backend.sln && dotnet publish backend.sln -c Release -o out

# Копируем результаты публикации в финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/backend/out .
ENTRYPOINT ["dotnet", "backend.dll"] 