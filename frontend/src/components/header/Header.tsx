import React, { useState } from 'react';
import './Header.css';
import UserDropdown from './UserDropdown';

const Header: React.FC = () => {
  const [username, setUsername] = useState<string | undefined>('Username'); // Здесь будет логика получения имени пользователя

  const handleLogout = () => {
    // Здесь будет логика выхода из аккаунта
    setUsername(undefined);
  };

  return (
    <header className="App-header">
      <div className="header-container">
        <div className="header-main">
          <button className="home-button">Перейти на главную</button>
        </div>
        <div className="header-user">
          <UserDropdown 
            username={username}
            onLogout={handleLogout}
          />
        </div>
      </div>
    </header>
  );
};

export default Header; 