import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './Header.css';
import UserDropdown from './UserDropdown';
import NotificationsPopup from '../NotificationsPopup';
import { userService } from '../../services/userService';
import { useAuth } from '../../contexts/AuthContext';

interface HeaderProps {
  onProfileClick: () => void;
  onHomeClick: () => void;
}

const Header: React.FC<HeaderProps> = ({ onProfileClick, onHomeClick }) => {
  const { user, setUser } = useAuth();
  const [showInfo, setShowInfo] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);

  const handleLogout = () => {
    userService.logout();
    setUser(null);
  };

  return (
    <header className="App-header">
      <div className="header-container">
        <div className="header-main">
          <img src="/images/logo.png" alt="Логотип" className="header-logo" />
          <button className="home-button" onClick={onHomeClick}>Перейти на главную</button>
          <button className="info-button" onClick={() => setShowInfo(!showInfo)}>
            i
          </button>
          {showInfo && (
            <div className="info-popup">
              <p>Добро пожаловать на сайт 
                <br />
                <br />
                На данный момент сайт находится в разработке. Готова страница профиля, система сообщений и друзей.
                <br />
                <br />
                В скором времени будет добавлена возможность создавать группы, слушать музыку и многое другое.
              </p>
            </div>
          )}
        </div>
        <div className="header-user">
          {user && (
            <div className="notifications-container">
              <button 
                className="notifications-button" 
                onClick={() => setShowNotifications(!showNotifications)}
              >
                🔔
              </button>
              <NotificationsPopup 
                isOpen={showNotifications} 
                onClose={() => setShowNotifications(false)} 
              />
            </div>
          )}
          {user ? (
            <UserDropdown 
              user={user}
              onLogout={handleLogout}
              onProfileClick={onProfileClick}
            />
          ) : (
            <div className="auth-buttons">
              <Link to="/auth" className="login-button">Войти</Link>
              <Link to="/auth?register=true" className="register-button">Регистрация</Link>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header; 