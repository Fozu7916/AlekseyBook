import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Auth from './pages/Auth/Auth';
import Home from './pages/Home';
import './App.css';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/main" element={<Home />} />
          <Route path="/auth" element={<Auth />} />
          <Route path="/communities" element={<Home />} />
          <Route path="/friends" element={<Home />} />
          <Route path="/music" element={<Home />} />
          <Route path="/games" element={<Home />} />
          <Route path="/other" element={<Home />} />
          <Route path="/profile" element={<Home />} />
          <Route path="/profile/:username" element={<Home />} />
          <Route path="*" element={<Navigate to="/" />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
