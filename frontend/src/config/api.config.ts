export const API_CONFIG = {
    BASE_URL: 'http://localhost:5038',
    API_URL: 'http://localhost:5038/api',
    CHAT_HUB_URL: 'http://localhost:5038/chatHub',
    ONLINE_STATUS_HUB_URL: 'http://localhost:5038/onlineStatusHub',
    MEDIA_URL: 'http://localhost:5038'
} as const;

// Функция для получения полного URL медиа-файлов
export const getMediaUrl = (path: string | null | undefined): string => {
    if (!path) return '/images/default-avatar.svg';
    return `${API_CONFIG.MEDIA_URL}${path}`;
}; 