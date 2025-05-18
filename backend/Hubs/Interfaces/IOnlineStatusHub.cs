namespace backend.Hubs.Interfaces
{
    public interface IOnlineStatusHub
    {
        Task UpdateFocusState(bool isFocused);
        Task GetOnlineUsers();
    }
} 