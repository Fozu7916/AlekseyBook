import React from 'react';
import Header from '../components/header/Header';
import Footer from '../components/footer/Footer';
import './Home.css';

const Home: React.FC = () => {
  return (
    <div className="App">
      <Header />
      <main className="App-main">
        <div className="left-sidebar">
          <h2>Вкладки</h2>
          {/* Здесь будет содержимое левого блока */}
        </div>
        <div className="main-content">
          <h2>Открытая вкладка</h2>
          {/* Здесь будет основной контент */}
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default Home; 