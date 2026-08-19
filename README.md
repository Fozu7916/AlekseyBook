#AlekseyBook
📋 Status: Project successfully completed (Archived MVP). The live demo is currently offline, but you can easily spin it up locally by following the installation guide below.
![image](https://github.com/user-attachments/assets/d89f93c7-bf23-439d-ac3e-b31f622d0c37)
![image](https://github.com/user-attachments/assets/c00046e0-4184-476c-bc5e-1f8707dcd240)
![image](https://github.com/user-attachments/assets/f64140b0-b0f5-4242-b57a-0dbe8cf7f8dd)
![image](https://github.com/user-attachments/assets/c1000a09-bab7-4d57-88f7-7611ab0af391)
![ima ge](https://github.com/user-attachments/assets/8bf6b184-679b-40d4-9001-4771793289dc)
![image](https://github.com/user-attachments/assets/582a979b-2fed-4546-89b1-99394207b547)
![image](https://github.com/user-attachments/assets/b2357d12-2ba4-4566-a7d1-2da09bdd5b3a)
![image](https://github.com/user-attachments/assets/02838099-6aa8-4a1e-bf14-ea225b16baae)

Social A network with a modern interface and advanced functionality.

## Main Features

- 👥 User Profiles
- 💬 Real-Time Chat
- 👋 Friends System
- 📝 Posting
- 💭 Comments and Likes
- 🟢 Online Status Tracking
- 🔔 Notifications

## Technologies

### Frontend
- React
- TypeScript
- SignalR (for real-time communication)
- CSS Modules

### Backend
- ASP.NET Core
- Entity Framework Core
- MySQL
- SignalR
- JWT Authentication

## Installation and Launch

### Requirements
- .NET 7.0+
- Node.js 16+
- MySQL 8.0+

### Database Setup
1. Create a MySQL database
2. Update the connection string in `backend/appsettings.json`

### Starting the Backend
```bash
cd backend
dotnet restore
dotnet run
```

### Starting the Frontend
```bash
cd frontend
npm install
npm start
```

## Main Features

### Chat System
- Instant Messaging
- Typing Indicator
- Read Status
- Message History

### Friends System
- Sending Friend Requests
- Accepting/Rejecting Requests
- Friends List
- Searching for Users

### Post Feed
- Creating Posts
- Commenting
- Liking
- Uploading Media

### Online Status
- Tracking User Activity
- Automatic Status Updates
- Multiple Tab Support
- Automatic Connection Recovery

## Security
- JWT Authentication
- XSS Protection
- CSRF Tokens
- Secure Storage Passwords

## Performance
- Optimized SQL queries
- Caching
- Lazy image loading
- Infinite scrolling

## Development

### Project Structure
```
├── backend/ # .NET Core backend
│ ├── Controllers/ # API controllers
│ ├── Models/ # Data models
│ ├── Services/ # Business logic
│ └── Hubs/ # SignalR hubs
├── frontend/ # React frontend
│ ├── src/
│ │ ├── components/
│ │ ├── pages/
│ │ ├── services/
│ │ └── contexts/
│ └── public/
```

### Code Style
- Adhere to SOLID principles
- Use linters
- Write tests for critical functionality
- Document the API

## License
MIT

## 🤝 Contribute to the Project

We welcome your contributions to the project! Please read our contributor guidelines before starting.

## 📝 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 📞 Contact

tg: @FozuZXC

If you have questions or suggestions, please create an issue in this repository.
