import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './UserDropdown.css';

interface UserDropdownProps {
  username?: string;
  onLogout: () => void;
}

const UserDropdown: React.FC<UserDropdownProps> = ({ username, onLogout }) => {
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

  if (!username) {
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
    <div className="user-dropdown" ref={dropdownRef}>
      <div 
        className="user-name"
        onClick={() => setIsOpen(!isOpen)}
      >
        {username}
        <div className="dropdown-arrow">▼</div>
      </div>
      
      {isOpen && (
        <div className="dropdown-menu">
          <div className="dropdown-item">Профиль</div>
          <div className="dropdown-item">Настройки</div>
          <div className="dropdown-divider"></div>
          <div className="dropdown-item" onClick={onLogout}>Выйти</div>
        </div>
      )}
    </div>
  );
};

export default UserDropdown; 