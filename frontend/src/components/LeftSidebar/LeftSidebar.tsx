import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { TabType } from '../../pages/tabs/types';
import './LeftSidebar.css';
import { userService, ChatPreview } from '../../services/userService';

interface LeftSidebarProps {
  activeTab: TabType;
  onTabChange: (tab: TabType) => void;
}

const LeftSidebar: React.FC<LeftSidebarProps> = ({ activeTab, onTabChange }) => {
  const navigate = useNavigate();
  const [unansweredCount, setUnansweredCount] = useState(0);

  const menuItems = [
    { id: 'main' as TabType, label: 'Новости', path: '/main' },
    { id: 'messages' as TabType, label: 'Сообщения', path: '/messages' },
    { id: 'friends' as TabType, label: 'Друзья', path: '/friends' },
    { id: 'music' as TabType, label: 'Музыка', path: '/music' },
    { id: 'games' as TabType, label: 'Игры', path: '/games' },
    { id: 'other' as TabType, label: 'Прочее', path: '/other' }
  ];

  useEffect(() => {
    const fetchChats = async () => {
      try {
        const chats: ChatPreview[] = await userService.getUserChats();
        // Считаем, в скольких чатах есть непрочитанные сообщения, на которые ты не ответил
        const currentUser = JSON.parse(localStorage.getItem('user') || '{}');
        const count = chats.filter(chat => {
          // Последнее сообщение не от тебя и не прочитано тобой
          return chat.lastMessage && chat.lastMessage.sender.id !== currentUser.id && chat.unreadCount > 0;
        }).length;
        setUnansweredCount(count);
      } catch (e) {
        setUnansweredCount(0);
      }
    };
    fetchChats();
    const interval = setInterval(fetchChats, 30000);
    return () => clearInterval(interval);
  }, []);

  const handleClick = (tab: TabType, path: string) => {
    onTabChange(tab);
    navigate(path);
  };

  return (
    <div className="left-sidebar">
      {menuItems.map(item => (
        <div
          key={item.id}
          className={`left-sidebar-item ${activeTab === item.id ? 'active' : ''}`}
          onClick={() => handleClick(item.id, item.path)}
        >
          {item.label}
          {(item.id as string) === 'messages' && unansweredCount > 0 && (
            <span className="sidebar-unanswered-badge">{unansweredCount}</span>
          )}
        </div>
      ))}
    </div>
  );
};

export default LeftSidebar; 