import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const MessagesTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Сообщества</h2>
        <p>Содержимое вкладки сообществ</p>
      </div>
    </div>
  );
};

export default MessagesTab; 