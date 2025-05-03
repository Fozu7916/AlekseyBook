import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

jest.mock('react-router-dom');

jest.mock('./contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

test('renders app', () => {
  render(<App />);
  expect(screen.getByText(/Router Mock/i)).toBeInTheDocument();
});
