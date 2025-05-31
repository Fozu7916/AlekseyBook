namespace backend
{
    public static class Config
    {
        // Base URLs
        public static string FrontendUrl => Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
        public static string BackendUrl => Environment.GetEnvironmentVariable("BACKEND_URL") ?? "http://localhost:5038";
        public static string ApiUrl => $"{BackendUrl}/api";

        // Hub URLs
        public static string ChatHubUrl => $"{BackendUrl}/chatHub";
        public static string OnlineStatusHubUrl => $"{BackendUrl}/onlineStatusHub";

        // Media URLs
        public static string MediaUrl => BackendUrl;
        public static string DefaultAvatarPath => "/images/default-avatar.svg";

        // API Endpoints
        public static class Endpoints
        {
            // Auth endpoints
            public static string Login => $"{ApiUrl}/auth/login";
            public static string Register => $"{ApiUrl}/auth/register";

            // User endpoints
            public static string Users => $"{ApiUrl}/users";
            public static string UserByUsername(string username) => $"{Users}/username/{username}";
            public static string UserById(int id) => $"{Users}/{id}";
            public static string UserAvatar(int id) => $"{Users}/{id}/avatar";

            // Friends endpoints
            public static string Friends => $"{ApiUrl}/friends";
            public static string FriendRequest(int friendId) => $"{Friends}/{friendId}";
            public static string AcceptFriend(int friendId) => $"{Friends}/{friendId}/accept";
            public static string DeclineFriend(int friendId) => $"{Friends}/{friendId}/decline";
            public static string BlockUser(int userId) => $"{Friends}/{userId}/block";
            public static string UserFriends(int userId) => $"{Friends}/user/{userId}";
            public static string FriendshipStatus(int friendId) => $"{Friends}/{friendId}/status";

            // Messages endpoints
            public static string Messages => $"{ApiUrl}/messages";
            public static string ChatMessages(int userId) => $"{Messages}/chat/{userId}";
            public static string ReadMessages(int userId) => $"{Messages}/read/{userId}";
            public static string UnreadCount => $"{Messages}/unread/count";
            public static string Chats => $"{Messages}/chats";

            // Wall posts endpoints
            public static string WallPosts => $"{ApiUrl}/wall-posts";
            public static string UserPosts(int userId) => $"{WallPosts}/user/{userId}";
            public static string PostById(int postId) => $"{WallPosts}/{postId}";

            // Comments and likes endpoints
            public static string PostLikes(int postId) => $"{ApiUrl}/LikeComment/posts/{postId}/likes";
            public static string PostComments(int postId) => $"{ApiUrl}/LikeComment/posts/{postId}/comments";
            public static string CommentById(int commentId) => $"{ApiUrl}/LikeComment/comments/{commentId}";
            public static string CommentLike(int commentId) => $"{ApiUrl}/LikeComment/comments/{commentId}/like";
        }

        public static class Urls
        {
            // Base URLs
            public static string BackendUrl => "https://localhost:7000";
            public static string FrontendUrl => "http://localhost:3000";
            public static string ApiUrl => $"{BackendUrl}/api";

            // Hub URLs
            public static string ChatHubUrl => $"{BackendUrl}/hubs/chat";
            public static string OnlineStatusHubUrl => $"{BackendUrl}/hubs/online-status";
            public static string NotificationHubUrl => $"{BackendUrl}/hubs/notification";

            // Auth endpoints
            public static string Login => $"{ApiUrl}/auth/login";
            public static string Register => $"{ApiUrl}/auth/register";
            public static string Logout => $"{ApiUrl}/auth/logout";

            // User endpoints
            public static string Users => $"{ApiUrl}/users";
            public static string UserById(int id) => $"{Users}/{id}";
            public static string UserProfile => $"{Users}/profile";
            public static string UserSearch(string query) => $"{Users}/search?query={query}";

            // Friend endpoints
            public static string Friends => $"{ApiUrl}/friends";
            public static string FriendRequest(int friendId) => $"{Friends}/request/{friendId}";
            public static string AcceptFriend(int friendId) => $"{Friends}/accept/{friendId}";
            public static string DeclineFriend(int friendId) => $"{Friends}/decline/{friendId}";
            public static string RemoveFriend(int friendId) => $"{Friends}/remove/{friendId}";
            public static string BlockUser(int userId) => $"{Friends}/block/{userId}";

            // Messages endpoints
            public static string Messages => $"{ApiUrl}/messages";
            public static string ChatMessages(int userId) => $"{Messages}/chat/{userId}";
            public static string ReadMessages(int userId) => $"{Messages}/read/{userId}";
            public static string UnreadCount => $"{Messages}/unread/count";
            public static string Chats => $"{Messages}/chats";

            // Notification endpoints
            public static string Notifications => $"{ApiUrl}/notifications";
            public static string NotificationRead(int id) => $"{Notifications}/{id}/read";
            public static string NotificationReadAll => $"{Notifications}/read-all";
            public static string NotificationUnreadCount => $"{Notifications}/unread/count";
        }
    }
} 