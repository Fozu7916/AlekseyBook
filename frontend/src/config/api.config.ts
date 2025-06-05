const getBaseUrl = () => {
  if (process.env.NODE_ENV === 'production') {
    return process.env.REACT_APP_API_URL || 'https://your-railway-app-url.railway.app';
  }
  return 'http://localhost:5038';
};

export const API_CONFIG = {
    BASE_URL: getBaseUrl(),
    API_URL: `${getBaseUrl()}/api`,
    CHAT_HUB_URL: `${getBaseUrl()}/chatHub`,
    ONLINE_STATUS_HUB_URL: `${getBaseUrl()}/onlineStatusHub`,
    MEDIA_URL: getBaseUrl()
} as const;

// Функция для получения полного URL медиа-файлов
export const getMediaUrl = (path: string | null | undefined): string => {
    if (!path) return '/images/default-avatar.svg';
    return `${API_CONFIG.MEDIA_URL}${path}`;
}; 