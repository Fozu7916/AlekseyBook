import React, { useState, useEffect } from 'react';
import { Notification } from '../types/notification';
import { NotificationItem } from './NotificationItem';
import { notificationService } from '../services/notificationService';
import { notificationHubService } from '../services/notificationHubService';
import './NotificationsList.css';

interface NotificationsListProps {
    isOpen: boolean;
    onClose: () => void;
}

export const NotificationsList: React.FC<NotificationsListProps> = ({ isOpen, onClose }) => {
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadNotifications = async () => {
        try {
            setLoading(true);
            const data = await notificationService.getNotifications();
            setNotifications(data);
        } catch (err) {
            setError('Ошибка при загрузке уведомлений');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (isOpen) {
            loadNotifications();
            
            // Подключаемся к хабу при открытии
            notificationHubService.connect();

            // Подписываемся на события
            const unsubscribeNew = notificationHubService.onNotification((notification) => {
                setNotifications(prev => [notification, ...prev]);
            });

            const unsubscribeRead = notificationHubService.onNotificationRead((notificationId) => {
                setNotifications(prev =>
                    prev.map(notification =>
                        notification.id === notificationId
                            ? { ...notification, isRead: true }
                            : notification
                    )
                );
            });

            const unsubscribeAllRead = notificationHubService.onAllNotificationsRead(() => {
                setNotifications(prev =>
                    prev.map(notification => ({ ...notification, isRead: true }))
                );
            });

            const unsubscribeDeleted = notificationHubService.onNotificationDeleted((notificationId) => {
                setNotifications(prev =>
                    prev.filter(notification => notification.id !== notificationId)
                );
            });

            // Отписываемся при закрытии
            return () => {
                unsubscribeNew();
                unsubscribeRead();
                unsubscribeAllRead();
                unsubscribeDeleted();
            };
        }
    }, [isOpen]);

    const handleMarkAsRead = async (id: number) => {
        try {
            await notificationService.markAsRead(id);
            setNotifications(prev =>
                prev.map(notification =>
                    notification.id === id
                        ? { ...notification, isRead: true }
                        : notification
                )
            );
        } catch (err) {
            console.error('Ошибка при отметке уведомления как прочитанного:', err);
        }
    };

    const handleDelete = async (id: number) => {
        try {
            await notificationService.deleteNotification(id);
            setNotifications(prev =>
                prev.filter(notification => notification.id !== id)
            );
        } catch (err) {
            console.error('Ошибка при удалении уведомления:', err);
        }
    };

    const handleMarkAllAsRead = async () => {
        try {
            await notificationService.markAllAsRead();
            setNotifications(prev =>
                prev.map(notification => ({ ...notification, isRead: true }))
            );
        } catch (err) {
            console.error('Ошибка при отметке всех уведомлений как прочитанных:', err);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="notifications-overlay">
            <div className="notifications-panel">
                <div className="notifications-header">
                    <h2 className="notifications-title">Уведомления</h2>
                    <div className="notifications-actions">
                        <button
                            onClick={handleMarkAllAsRead}
                            className="notifications-mark-read"
                        >
                            Прочитать все
                        </button>
                        <button
                            onClick={onClose}
                            className="notifications-close"
                        >
                            ✕
                        </button>
                    </div>
                </div>
                
                <div className="notifications-content">
                    {loading ? (
                        <div className="notifications-loading">
                            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
                        </div>
                    ) : error ? (
                        <div className="notifications-error">{error}</div>
                    ) : notifications.length === 0 ? (
                        <div className="notifications-empty">
                            Нет уведомлений
                        </div>
                    ) : (
                        notifications.map(notification => (
                            <NotificationItem
                                key={notification.id}
                                notification={notification}
                                onMarkAsRead={handleMarkAsRead}
                                onDelete={handleDelete}
                            />
                        ))
                    )}
                </div>
            </div>
        </div>
    );
}; 