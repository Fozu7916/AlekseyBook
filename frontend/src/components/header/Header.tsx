import React, { useState, useEffect } from 'react';
import './Header.css';
import UserDropdown from './UserDropdown';
import { userService, User } from '../../services/userService';

interface HeaderProps {
  onProfileClick: () => void;
  onHomeClick: () => void;
}

const Header: React.FC<HeaderProps> = ({ onProfileClick, onHomeClick }) => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  useEffect(() => {
    const fetchUser = async () => {
      const user = await userService.getCurrentUser();
      setCurrentUser(user);
    };

    fetchUser();
  }, []);

  const handleLogout = () => {
    userService.logout();
    setCurrentUser(null);
    window.location.reload(); // Перезагружаем страницу для сброса всех состояний
  };

  return (
    <header className="App-header">
      <div className="header-container">
        <div className="header-main">
          <button className="home-button" onClick={onHomeClick}>Перейти на главную</button>
        </div>
        <div className="header-user">
          <UserDropdown 
            user={currentUser}
            onLogout={handleLogout}
            onProfileClick={onProfileClick}
          />
        </div>
      </div>
    </header>
  );
};

export default Header; 