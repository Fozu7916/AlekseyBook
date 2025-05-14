namespace backend
{
    public static class Config
    {
        public static string FrontendUrl => "http://localhost:3000";
        public static string BackendUrl => "http://localhost:5038";
        public static string ApiUrl => $"{BackendUrl}/api";
    }
} 