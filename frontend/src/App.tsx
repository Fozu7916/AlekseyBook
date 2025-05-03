import React from 'react';
import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import Auth from './pages/Auth/Auth';
import Home from './pages/Home';
import './App.css';

const router = createBrowserRouter([
  {
    path: '/',
    element: <Home />
  },
  {
    path: '/main',
    element: <Home />
  },
  {
    path: '/auth',
    element: <Auth />
  },
  {
    path: '/messages',
    element: <Home />
  },
  {
    path: '/messages/:userId',
    element: <Home />
  },
  {
    path: '/friends',
    element: <Home />
  },
  {
    path: '/music',
    element: <Home />
  },
  {
    path: '/games',
    element: <Home />
  },
  {
    path: '/other',
    element: <Home />
  },
  {
    path: '/profile',
    element: <Home />
  },
  {
    path: '/profile/:username',
    element: <Home />
  },
  {
    path: '*',
    element: <Navigate to="/" />
  }
], {
  future: {
    v7_relativeSplatPath: true
  }
});

function App() {
  return (
    <AuthProvider>
      <div className="App">
        <RouterProvider router={router} />
      </div>
    </AuthProvider>
  );
}

export default App;
