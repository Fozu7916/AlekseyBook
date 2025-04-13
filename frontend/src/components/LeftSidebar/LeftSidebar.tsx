import React from 'react';
import { useNavigate } from 'react-router-dom';
import { TabType } from '../../pages/tabs/types';
import './LeftSidebar.css';

interface LeftSidebarProps {
  activeTab: TabType;
  onTabChange: (tab: TabType) => void;
}

const LeftSidebar: React.FC<LeftSidebarProps> = ({ activeTab, onTabChange }) => {
  const navigate = useNavigate();

  const menuItems = [
    { id: 'main' as TabType, label: 'Новости', path: '/main' },
    { id: 'communities' as TabType, label: 'Сообщества', path: '/communities' },
    { id: 'friends' as TabType, label: 'Друзья', path: '/friends' },
    { id: 'music' as TabType, label: 'Музыка', path: '/music' },
    { id: 'games' as TabType, label: 'Игры', path: '/games' },
    { id: 'other' as TabType, label: 'Прочее', path: '/other' }
  ];

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
        </div>
      ))}
    </div>
  );
};

export default LeftSidebar; 