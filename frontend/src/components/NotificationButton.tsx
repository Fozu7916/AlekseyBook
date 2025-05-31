import React, { useState, useEffect } from 'react';
import { notificationService } from '../services/notificationService';
import { NotificationsList } from './NotificationsList';
import { notificationHubService } from '../services/notificationHubService';

export const NotificationButton: React.FC = () => {
    const [isOpen, setIsOpen] = useState(false);
    const [unreadCount, setUnreadCount] = useState(0);

    const loadUnreadCount = async () => {
        try {
            const count = await notificationService.getUnreadCount();
            setUnreadCount(count);
        } catch (err) {
            console.error('Ошибка при получении количества непрочитанных уведомлений:', err);
        }
    };

    useEffect(() => {
        loadUnreadCount();
        const interval = setInterval(loadUnreadCount, 30000); // Обновляем каждые 30 секунд

        // Подписка на SignalR события
        const unsubNew = notificationHubService.onNotification(() => {
            setUnreadCount(count => count + 1);
        });
        const unsubAllRead = notificationHubService.onAllNotificationsRead(() => {
            setUnreadCount(0);
        });

        return () => {
            clearInterval(interval);
            unsubNew();
            unsubAllRead();
        };
    }, []);

    return (
        <>
            <button
                onClick={() => setIsOpen(true)}
                className="relative p-2 text-gray-600 hover:text-gray-900 focus:outline-none"
            >
                <span className="text-2xl">🔔</span>
                {unreadCount > 0 && (
                    <span className="absolute top-0 right-0 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white transform translate-x-1/2 -translate-y-1/2 bg-red-600 rounded-full">
                        {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                )}
            </button>
            <NotificationsList
                isOpen={isOpen}
                onClose={() => {
                    setIsOpen(false);
                    loadUnreadCount(); // Обновляем счетчик при закрытии
                }}
            />
        </>
    );
}; 