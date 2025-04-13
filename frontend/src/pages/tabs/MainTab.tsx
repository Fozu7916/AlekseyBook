import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const ProfileTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Главная</h2>
        <p>Содержимое вкладки моей страницы</p>
      </div>
    </div>
  );
};

export default ProfileTab; 