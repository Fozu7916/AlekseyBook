import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Avatar } from '../Avatar';
import './UserDropdown.css';
import { User } from '../../services/userService';

interface UserDropdownProps {
  user?: User | null;
  onLogout: () => void;
  onProfileClick: () => void;
}

const UserDropdown: React.FC<UserDropdownProps> = ({ user, onLogout, onProfileClick }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  if (!user) {
    return (
      <div className="auth-buttons">
        <button 
          className="auth-button login"
          onClick={() => navigate('/auth')}
        >
          Войти
        </button>
        <button 
          className="auth-button register"
          onClick={() => navigate('/auth?register=true')}
        >
          Зарегистрироваться
        </button>
      </div>
    );
  }

  return (
    <div className={`user-dropdown ${isOpen ? 'open' : ''}`} ref={dropdownRef}>
      <button className="user-button" onClick={() => setIsOpen(!isOpen)}>
        <Avatar 
          avatarUrl={user.avatarUrl}
          className="user-avatar"
          alt={user.username}
        />
      </button>
      
      {isOpen && (
        <div className="dropdown-menu">
          <div className="dropdown-item" onClick={() => {
            onProfileClick();
            setIsOpen(false);
          }}>Профиль</div>
          <div className="dropdown-item">Настройки</div>
          <div className="dropdown-divider"></div>
          <div className="dropdown-item" onClick={onLogout}>Выйти</div>
        </div>
      )}
    </div>
  );
};

export default UserDropdown; 