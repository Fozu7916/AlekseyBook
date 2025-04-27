import { render, screen, fireEvent, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider, useAuth } from '../AuthContext';
import { userService } from '../../services/userService';

jest.mock('../../services/userService', () => ({
  userService: {
    getCurrentUser: jest.fn(),
    login: jest.fn(),
    register: jest.fn(),
    logout: jest.fn()
  }
}));

const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  avatarUrl: '/test-avatar.jpg',
  status: 'active',
  isVerified: true,
  createdAt: new Date().toISOString()
};

const TestComponent = () => {
  const { user, login, logout } = useAuth();
  return (
    <div>
      {user ? (
        <>
          <div>Logged in as {user.username}</div>
          <button onClick={logout}>Logout</button>
        </>
      ) : (
        <>
          <button onClick={() => login({ email: 'test@example.com', password: 'password' })}>
            Login
          </button>
        </>
      )}
    </div>
  );
};

describe('AuthContext', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  const renderTestComponent = async () => {
    let component;
    await act(async () => {
      component = render(
        <BrowserRouter>
          <AuthProvider>
            <TestComponent />
          </AuthProvider>
        </BrowserRouter>
      );
    });
    return component;
  };

  test('provides authentication state', async () => {
    await renderTestComponent();
    expect(screen.getByText(/login/i)).toBeInTheDocument();
  });

  test('handles login successfully', async () => {
    (userService.login as jest.Mock).mockResolvedValueOnce({ user: mockUser });
    await renderTestComponent();
    
    const loginButton = screen.getByText(/login/i);
    await act(async () => {
      fireEvent.click(loginButton);
    });
    
    expect(await screen.findByText(`Logged in as ${mockUser.username}`)).toBeInTheDocument();
  });

  test('handles logout', async () => {
    (userService.getCurrentUser as jest.Mock).mockResolvedValueOnce(mockUser);
    localStorage.setItem('user', JSON.stringify(mockUser));
    
    await renderTestComponent();
    const logoutButton = await screen.findByText(/logout/i);
    
    await act(async () => {
      fireEvent.click(logoutButton);
    });
    
    expect(screen.getByText(/login/i)).toBeInTheDocument();
  });
}); 