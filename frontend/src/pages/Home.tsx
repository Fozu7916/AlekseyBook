import React, { useState } from 'react';
import './Home.css';
import ProfileTab from './tabs/ProfileTab';
import MessagesTab from './tabs/MessagesTab';
import FriendsTab from './tabs/FriendsTab';
import MusicTab from './tabs/MusicTab';
import SettingsTab from './tabs/SettingsTab';
import NotificationsTab from './tabs/NotificationsTab';
import { TabType } from './tabs/types';
import Header from '../components/header/Header';
import Footer from '../components/footer/Footer';

const Home: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>('myPage');

  const tabs: Record<TabType, React.ReactNode> = {
    myPage: <ProfileTab isActive={activeTab === 'myPage'} />,
    communities: <MessagesTab isActive={activeTab === 'communities'} />,
    friends: <FriendsTab isActive={activeTab === 'friends'} />,
    music: <MusicTab isActive={activeTab === 'music'} />,
    games: <SettingsTab isActive={activeTab === 'games'} />,
    other: <NotificationsTab isActive={activeTab === 'other'} />
  };

  const handleTabClick = (tab: TabType) => {
    setActiveTab(tab);
  };

  return (
    <div className="App">
      <Header />
      <div className="App-main">
        
        <div className="left-sidebar">
          <div 
            className={`left-sidebar-item ${activeTab === 'myPage' ? 'active' : ''}`}
            onClick={() => handleTabClick('myPage')}
          >
            Моя страница
          </div>

          <div 
            className={`left-sidebar-item ${activeTab === 'communities' ? 'active' : ''}`}
            onClick={() => handleTabClick('communities')}
          >
            Сообщества
          </div>

          <div 
            className={`left-sidebar-item ${activeTab === 'friends' ? 'active' : ''}`}
            onClick={() => handleTabClick('friends')}
          >
            Друзья
          </div>

          <div 
            className={`left-sidebar-item ${activeTab === 'music' ? 'active' : ''}`}
            onClick={() => handleTabClick('music')}
          >
            Музыка
          </div>

          <div 
            className={`left-sidebar-item ${activeTab === 'games' ? 'active' : ''}`}
            onClick={() => handleTabClick('games')}
          >
            Игры
          </div>

          <div 
            className={`left-sidebar-item ${activeTab === 'other' ? 'active' : ''}`}
            onClick={() => handleTabClick('other')}
          >
            Прочее
          </div>

        </div>
        <div className="main-content">
          {tabs[activeTab]}
        </div>
        
      </div>
      <Footer />
    </div>
  );
};

export default Home; 