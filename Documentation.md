# AlekseyBook Documentation

- [1. Overview](#1-overview)
- [2. Installation and Launch](#2-installation-and-launch)
- [3. Architecture](#3-architecture)
- [4. Features](#4-features)
- [5. Security](#5-security)
- [6. Performance](#6-performance)
- [7. Development](#7-development)

---

## 1. Overview

AlekseyBook is a social network built as an MVP (project archived). It includes user profiles, real-time chat, a friends system, a post feed with comments and likes, online status tracking, and notifications.

| Layer    | Technologies                                                           |
|----------|------------------------------------------------------------------------|
| Frontend | React, TypeScript, SignalR, CSS Modules                                |
| Backend  | ASP.NET Core, Entity Framework Core, MySQL, SignalR, JWT Authentication |

---

## 2. Installation and Launch

### Requirements

- .NET 7.0+
- Node.js 16+
- MySQL 8.0+

### Database

1. Create an empty MySQL database.
2. Set the connection string in `backend/appsettings.json`.

### Backend

```bash
cd backend
dotnet restore
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm start
```

---

## 3. Architecture

The backend exposes an HTTP API (controllers) and SignalR hubs for real-time events. The frontend is a React SPA that talks to both. Authentication is handled with JWT.

### Project Structure

```
├── backend/            # .NET Core backend
│   ├── Controllers/    # API controllers
│   ├── Models/         # Data models
│   ├── Services/       # Business logic
│   └── Hubs/           # SignalR hubs
├── frontend/           # React frontend
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   └── contexts/
│   └── public/
```

---

## 4. Features

### Chat
- Instant messaging
- Typing indicator
- Read status
- Message history

### Friends
- Sending friend requests
- Accepting / rejecting requests
- Friends list
- User search

### Posts
- Creating posts
- Comments
- Likes
- Media uploads

### Online Status
- User activity tracking
- Automatic status updates
- Multiple tab support
- Automatic connection recovery

---

## 5. Security

- JWT authentication
- XSS protection
- CSRF tokens
- Secure password storage

---

## 6. Performance

- Optimized SQL queries
- Caching
- Lazy image loading
- Infinite scrolling

---

## 7. Development

### Code Style
- Follow SOLID principles
- Use linters
- Write tests for critical functionality
- Document the API

### Contributing
1. Fork the repository and create a feature branch.
2. Follow the code style above.
3. Open a pull request with a clear description of the change.
