import React from 'react';
import './Header.css';

const Header: React.FC = () => {
  return (
    <header className="App-header">
      <div className="header-container">
        <div className="header-main">
          <button className="home-button">Перейти на главную</button>
        </div>
        <div className="header-user">
          <span className="user-name">Username</span>
        </div>
      </div>
    </header>
  );
};

export default Header; 