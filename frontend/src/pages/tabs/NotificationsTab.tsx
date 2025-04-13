import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const NotificationsTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Прочее</h2>
        <p>Содержимое вкладки прочего</p>
      </div>
    </div>
  );
};

export default NotificationsTab; 