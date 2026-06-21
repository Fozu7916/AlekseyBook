# AlekseyBook
📋 Status: Project successfully completed (Archived MVP). The live demo is currently offline, but you can easily spin it up locally by following the installation guide below.
![image](https://github.com/user-attachments/assets/d89f93c7-bf23-439d-ac3e-b31f622d0c37)
![image](https://github.com/user-attachments/assets/c00046e0-4184-476c-bc5e-1f8707dcd240)
![image](https://github.com/user-attachments/assets/f64140b0-b0f5-4242-b57a-0dbe8cf7f8dd)
![image](https://github.com/user-attachments/assets/c1000a09-bab7-4d57-88f7-7611ab0af391)
![image](https://github.com/user-attachments/assets/8bf6b184-679b-40d4-9001-4771793289dc)
![image](https://github.com/user-attachments/assets/582a979b-2fed-4546-89b1-99394207b547)
![image](https://github.com/user-attachments/assets/b2357d12-2ba4-4566-a7d1-2da09bdd5b3a)
![image](https://github.com/user-attachments/assets/02838099-6aa8-4a1e-bf14-ea225b16baae)

Социальная сеть с современным интерфейсом и расширенным функционалом.

## Основные функции

- 👥 Профили пользователей
- 💬 Чат в реальном времени
- 👋 Система друзей
- 📝 Публикация постов
- 💭 Комментарии и лайки
- 🟢 Отслеживание онлайн-статуса
- 🔔 Уведомления

## Технологии

### Фронтенд
- React
- TypeScript
- SignalR (для real-time коммуникации)
- CSS Modules

### Бэкенд
- ASP.NET Core
- Entity Framework Core
- MySQL
- SignalR
- JWT аутентификация

## Установка и запуск

### Требования
- .NET 7.0+
- Node.js 16+
- MySQL 8.0+

### Настройка базы данных
1. Создайте базу данных MySQL
2. Обновите строку подключения в `backend/appsettings.json`

### Запуск бэкенда
```bash
cd backend
dotnet restore
dotnet run
```

### Запуск фронтенда
```bash
cd frontend
npm install
npm start
```

## Основные возможности

### Система чата
- Мгновенные сообщения
- Индикатор печати
- Статус прочтения
- История сообщений

### Система друзей
- Отправка заявок в друзья
- Принятие/отклонение заявок
- Список друзей
- Поиск пользователей

### Лента постов
- Создание постов
- Комментирование
- Лайки
- Загрузка медиафайлов

### Онлайн-статус
- Отслеживание активности пользователей
- Автоматическое обновление статуса
- Поддержка множественных вкладок
- Автоматическое восстановление соединения

## Безопасность
- JWT аутентификация
- Защита от XSS
- CSRF токены
- Безопасное хранение паролей

## Производительность
- Оптимизированные SQL запросы
- Кэширование
- Ленивая загрузка изображений
- Бесконечная прокрутка

## Разработка

### Структура проекта
```
├── backend/            # .NET Core backend
│   ├── Controllers/   # API контроллеры
│   ├── Models/        # Модели данных
│   ├── Services/      # Бизнес-логика
│   └── Hubs/          # SignalR хабы
├── frontend/          # React frontend
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   └── contexts/
│   └── public/
```

### Стиль кода
- Придерживаемся принципов SOLID
- Используем линтеры
- Пишем тесты для критического функционала
- Документируем API

## Лицензия
MIT

## 🤝 Вклад в проект

Мы приветствуем ваш вклад в проект! Пожалуйста, ознакомьтесь с нашими правилами для контрибьюторов перед началом работы.

## 📝 Лицензия

Этот проект распространяется под лицензией MIT. Подробности смотрите в файле LICENSE.

## 📞 Контакты

tg: @FozuZXC

Если у вас есть вопросы или предложения, пожалуйста, создайте issue в этом репозитории. 
