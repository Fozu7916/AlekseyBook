import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './NotificationsPopup.css';

interface Notification {
  id: number;
  type: 'message' | 'friend' | 'system';
  title: string;
  text: string;
  timestamp: Date;
  read: boolean;
  link?: string;
}

interface NotificationsPopupProps {
  isOpen: boolean;
  onClose: () => void;
}

const NotificationsPopup: React.FC<NotificationsPopupProps> = ({ isOpen, onClose }) => {
  const [notifications, setNotifications] = useState<Notification[]>([
    {
      id: 1,
      type: 'message',
      title: 'Новое сообщение',
      text: 'Привет, как дела?',
      timestamp: new Date(),
      read: false,
      link: '/messages/1'
    },
    // Тестовые уведомления, потом удалить
    {
      id: 2,
      type: 'friend',
      title: 'Запрос в друзья',
      text: 'Иван хочет добавить вас в друзья',
      timestamp: new Date(Date.now() - 3600000),
      read: true,
      link: '/friends'
    }
  ]);
  const popupRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (popupRef.current && !popupRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen, onClose]);

  const handleNotificationClick = (notification: Notification) => {
    if (notification.link) {
      navigate(notification.link);
      onClose();
    }
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'message':
        return '💬';
      case 'friend':
        return '👥';
      case 'system':
        return '🔔';
      default:
        return '📌';
    }
  };

  const formatTimestamp = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return 'только что';
    if (minutes < 60) return `${minutes} мин. назад`;
    if (hours < 24) return `${hours} ч. назад`;
    if (days === 1) return 'вчера';
    return date.toLocaleDateString();
  };

  if (!isOpen) return null;

  return (
    <div className="notifications-popup" ref={popupRef}>
      <div className="notifications-header">
        <h3>Уведомления</h3>
        <button className="mark-all-read">Прочитать все</button>
      </div>
      <div className="notifications-list">
        {notifications.length > 0 ? (
          notifications.map(notification => (
            <div
              key={notification.id}
              className={`notification-item ${notification.read ? 'read' : 'unread'}`}
              onClick={() => handleNotificationClick(notification)}
            >
              <div className="notification-icon">
                {getNotificationIcon(notification.type)}
              </div>
              <div className="notification-content">
                <div className="notification-title">{notification.title}</div>
                <div className="notification-text">{notification.text}</div>
                <div className="notification-time">
                  {formatTimestamp(notification.timestamp)}
                </div>
              </div>
              {!notification.read && <div className="unread-dot" />}
            </div>
          ))
        ) : (
          <div className="no-notifications">
            Нет новых уведомлений
          </div>
        )}
      </div>
    </div>
  );
};

export default NotificationsPopup; 