namespace backend.Hubs.Interfaces
{
    public interface IOnlineStatusHub
    {
        Task GetOnlineUsers();
        Task UpdateActivity();
    }
} 