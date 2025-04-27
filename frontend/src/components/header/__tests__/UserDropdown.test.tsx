import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import UserDropdown from '../UserDropdown';

const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  avatarUrl: '/test-avatar.jpg',
  status: 'active',
  isVerified: true,
  createdAt: new Date().toISOString()
};

describe('UserDropdown Component', () => {
  const mockProps = {
    user: mockUser,
    onLogout: jest.fn(),
    onProfileClick: jest.fn()
  };

  const renderDropdown = () => {
    return render(
      <BrowserRouter>
        <UserDropdown {...mockProps} />
      </BrowserRouter>
    );
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders user information', () => {
    renderDropdown();
    
    expect(screen.getByText(mockUser.username)).toBeInTheDocument();
    expect(screen.getByAltText(mockUser.username)).toBeInTheDocument();
  });

  test('opens dropdown menu on click', () => {
    renderDropdown();
    
    const userButton = screen.getByText(mockUser.username);
    fireEvent.click(userButton);
    
    expect(screen.getByText('Профиль')).toBeInTheDocument();
    expect(screen.getByText('Настройки')).toBeInTheDocument();
    expect(screen.getByText('Выйти')).toBeInTheDocument();
  });

  test('handles profile click', () => {
    renderDropdown();
    
    const userButton = screen.getByText(mockUser.username);
    fireEvent.click(userButton);
    
    const profileButton = screen.getByText('Профиль');
    fireEvent.click(profileButton);
    
    expect(mockProps.onProfileClick).toHaveBeenCalled();
  });

  test('handles logout click', () => {
    renderDropdown();
    
    const userButton = screen.getByText(mockUser.username);
    fireEvent.click(userButton);
    
    const logoutButton = screen.getByText('Выйти');
    fireEvent.click(logoutButton);
    
    expect(mockProps.onLogout).toHaveBeenCalled();
  });

  test('closes dropdown when clicking outside', () => {
    renderDropdown();
    
    // Открываем дропдаун
    const userButton = screen.getByText(mockUser.username);
    fireEvent.click(userButton);
    
    // Проверяем, что меню открыто
    expect(screen.getByText('Профиль')).toBeInTheDocument();
    
    // Кликаем вне дропдауна
    fireEvent.mouseDown(document.body);
    
    // Проверяем, что меню закрыто
    expect(screen.queryByText('Профиль')).not.toBeInTheDocument();
  });

  test('renders default avatar when avatarUrl is not provided', () => {
    const userWithoutAvatar = { ...mockUser, avatarUrl: undefined };
    render(
      <BrowserRouter>
        <UserDropdown {...mockProps} user={userWithoutAvatar} />
      </BrowserRouter>
    );
    
    const avatar = screen.getByAltText(mockUser.username);
    expect(avatar.getAttribute('src')).toContain('default-avatar.svg');
  });
}); 