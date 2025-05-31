import React from 'react';
import { Notification } from '../types/notification';
import { Link, useNavigate } from 'react-router-dom';

interface NotificationItemProps {
    notification: Notification;
    onMarkAsRead: (id: number) => void;
    onDelete: (id: number) => void;
}

const getNotificationIcon = (type: string) => {
    switch (type) {
        case 'Message':
            return '💬';
        case 'Friend':
            return '👥';
        case 'System':
            return '🔔';
        default:
            return '📌';
    }
};

const formatDateTime = (dateStr: string) => {
    const date = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    
    // Меньше минуты
    if (diff < 60000) {
        return 'только что';
    }
    
    // Меньше часа
    if (diff < 3600000) {
        const minutes = Math.floor(diff / 60000);
        return `${minutes} ${minutes === 1 ? 'минуту' : minutes < 5 ? 'минуты' : 'минут'} назад`;
    }
    
    // Сегодня
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (date >= today) {
        return `сегодня в ${date.toLocaleTimeString('ru-RU', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false 
        })}`;
    }
    
    // Вчера
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    if (date >= yesterday) {
        return `вчера в ${date.toLocaleTimeString('ru-RU', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false
        })}`;
    }
    
    // Более старая дата
    return date.toLocaleDateString('ru-RU', { 
        day: 'numeric',
        month: 'long',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    });
};

export const NotificationItem: React.FC<NotificationItemProps> = ({
    notification,
    onMarkAsRead,
    onDelete
}) => {
    const navigate = useNavigate();
    const handleClick = (e?: React.MouseEvent) => {
        if (!notification.isRead) {
            onMarkAsRead(notification.id);
        }
        if (notification.link) {
            if (notification.link.startsWith('/messages')) {
                navigate('/messages');
            } else if (notification.link.startsWith('/friends')) {
                navigate('/friends');
            } else if (notification.link.startsWith('/profile')) {
                navigate(notification.link);
            } else if (notification.link.startsWith('/music')) {
                navigate('/music');
            } else if (notification.link.startsWith('/main')) {
                navigate('/main');
            } else {
                navigate(notification.link);
            }
            if (e) e.preventDefault();
        }
    };

    const content = (
        <div 
            className={`notification-item ${!notification.isRead ? 'unread' : ''}`}
            onClick={handleClick}
        >
            <div className="notification-icon">
                {getNotificationIcon(notification.type)}
            </div>
            <div className="notification-body">
                <div className="flex justify-between">
                    <h3 className="notification-title">
                        {notification.title}
                    </h3>
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            onDelete(notification.id);
                        }}
                        className="notification-delete"
                    >
                        ✕
                    </button>
                </div>
                <p className="notification-text">{notification.text}</p>
                <div className="notification-time">
                    {formatDateTime(notification.createdAt)}
                </div>
            </div>
        </div>
    );

    return notification.link ? (
        <a href={notification.link} onClick={handleClick}>{content}</a>
    ) : (
        content
    );
}; 