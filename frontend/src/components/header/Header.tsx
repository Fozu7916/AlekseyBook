import React from 'react';
import { Link } from 'react-router-dom';
import './Header.css';
import UserDropdown from './UserDropdown';
import { userService } from '../../services/userService';
import { useAuth } from '../../contexts/AuthContext';

interface HeaderProps {
  onProfileClick: () => void;
  onHomeClick: () => void;
}

const Header: React.FC<HeaderProps> = ({ onProfileClick, onHomeClick }) => {
  const { user, setUser } = useAuth();

  const handleLogout = () => {
    userService.logout();
    setUser(null);
  };

  return (
    <header className="App-header">
      <div className="header-container">
        <div className="header-main">
          <button className="home-button" onClick={onHomeClick}>Перейти на главную</button>
        </div>
        <div className="header-user">
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