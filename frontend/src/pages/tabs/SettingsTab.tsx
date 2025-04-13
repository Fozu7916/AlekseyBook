import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const SettingsTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Игры</h2>
        <p>Содержимое вкладки игр</p>
      </div>
    </div>
  );
};

export default SettingsTab; 