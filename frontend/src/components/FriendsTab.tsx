import React, { useState } from 'react';
import FriendsList from './FriendsList';
import UsersList from './UsersList';
import './FriendsTab.css';

const FriendsTab: React.FC = () => {
    const [activeTab, setActiveTab] = useState(0);

    return (
        <div className="friends-tab-container">
            <div className="tabs">
                <button
                    className={`tab-button ${activeTab === 0 ? 'active' : ''}`}
                    onClick={() => setActiveTab(0)}
                >
                    Друзья
                </button>
                <button
                    className={`tab-button ${activeTab === 1 ? 'active' : ''}`}
                    onClick={() => setActiveTab(1)}
                >
                    Все пользователи
                </button>
            </div>
            <div className="tab-content">
                {activeTab === 0 && <FriendsList />}
                {activeTab === 1 && <UsersList />}
            </div>
        </div>
    );
};

export default FriendsTab; 