import React from 'react';
import { render } from '@testing-library/react';
import App from './App';

jest.mock('react-router-dom');

jest.mock('./contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

test('renders App component', () => {
  render(<App />);
});
