import React from 'react';
import './Tabs.css';
import { TabProps } from './types';

const FriendsTab: React.FC<TabProps> = ({ isActive }) => {
  return (
    <div className={`tab ${isActive ? 'active' : ''}`}>
      <div className="tab-content">
        <h2 className="tab-title">Друзья</h2>
        <p>Содержимое вкладки друзей</p>
      </div>
    </div>
  );
};

export default FriendsTab; 