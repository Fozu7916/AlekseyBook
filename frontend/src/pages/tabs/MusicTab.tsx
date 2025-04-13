import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const MusicTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Музыка</h2>
        <p>Содержимое вкладки музыки</p>
      </div>
    </div>
  );
};

export default MusicTab; 