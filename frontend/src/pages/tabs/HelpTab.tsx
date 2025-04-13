import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const HelpTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Помощь</h2>
        <p>Содержимое вкладки помощи</p>
      </div>
    </div>
  );
};

export default HelpTab; 