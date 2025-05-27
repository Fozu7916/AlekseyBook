export type NotificationType = 'Message' | 'Friend' | 'System' | 'Comment' | 'Like';

export interface Notification {
    id: number;
    type: NotificationType;
    title: string;
    text: string;
    link?: string;
    isRead: boolean;
    createdAt: string;
}

export interface NotificationResponse {
    notifications: Notification[];
    unreadCount: number;
}

export const NOTIFICATION_TYPES = {
    FRIEND_REQUEST: 'Friend',
    FRIEND_ACCEPTED: 'Friend',
    COMMENT: 'Comment',
    COMMENT_REPLY: 'Comment',
    POST_LIKE: 'Like',
    COMMENT_LIKE: 'Like'
} as const; 