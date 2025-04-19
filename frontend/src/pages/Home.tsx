import React, { useState, useEffect } from 'react';
import { useParams, useLocation, useNavigate } from 'react-router-dom';
import './Home.css';
import MainTab from './tabs/MainTab';
import MessagesTab from './tabs/MessagesTab';
import FriendsTab from './tabs/FriendsTab';
import MusicTab from './tabs/MusicTab';
import SettingsTab from './tabs/SettingsTab';
import NotificationsTab from './tabs/NotificationsTab';
import ProfileTab from './tabs/ProfileTab';
import { TabType } from './tabs/types';
import Header from '../components/header/Header';
import Footer from '../components/footer/Footer';
import LeftSidebar from '../components/LeftSidebar/LeftSidebar';
import { useAuth } from '../contexts/AuthContext';

const getTabFromPath = (pathname: string): TabType => {
  const path = pathname.split('/')[1];
  switch (path) {
    case 'main':
      return 'main';
    case 'messages':
      return 'message';
    case 'friends':
      return 'friends';
    case 'music':
      return 'music';
    case 'games':
      return 'games';
    case 'other':
      return 'other';
    case 'profile':
      return 'profile';
    default:
      return 'main';
  }
};

const Home: React.FC = () => {
  const { username: urlUsername } = useParams<{ username?: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<TabType>(getTabFromPath(location.pathname));
  const { user } = useAuth();

  useEffect(() => {
    setActiveTab(getTabFromPath(location.pathname));
  }, [location.pathname]);

  const handleTabChange = (tab: TabType) => {
    setActiveTab(tab);
    if (tab === 'profile' && user) {
      navigate(`/profile/${user.username}`);
    } else if (tab === 'message') {
      navigate('/messages');
    } else {
      navigate(`/${tab}`);
    }
  };

  const tabs: Record<TabType, React.ReactNode> = {
    main: <MainTab isActive={activeTab === 'main'} />,
    message: <MessagesTab isActive={activeTab === 'message'} />,
    friends: <FriendsTab isActive={activeTab === 'friends'} />,
    music: <MusicTab isActive={activeTab === 'music'} />,
    games: <SettingsTab isActive={activeTab === 'games'} />,
    other: <NotificationsTab isActive={activeTab === 'other'} />,
    profile: <ProfileTab isActive={activeTab === 'profile'} username={urlUsername || user?.username || ''} />
  };

  return (
    <div className="App">
      <Header 
        onProfileClick={() => handleTabChange('profile')}
        onHomeClick={() => {
          setActiveTab('main');
          navigate('/main');
        }}
      />
      <div className="App-main">
        <LeftSidebar 
          activeTab={activeTab}
          onTabChange={handleTabChange}
        />
        <div className="main-content">
          {tabs[activeTab]}
        </div>
      </div>
      <Footer />
    </div>
  );
};

export default Home; 