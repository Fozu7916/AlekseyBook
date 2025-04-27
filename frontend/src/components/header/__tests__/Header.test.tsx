import { render, screen, fireEvent, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../../../contexts/AuthContext';
import Header from '../Header';
import { userService } from '../../../services/userService';

const mockNavigate = jest.fn();

// Мокаем react-router
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

// Мокаем сервисы
jest.mock('../../../services/userService', () => ({
  userService: {
    logout: jest.fn(),
    getCurrentUser: jest.fn()
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

describe('Header Component', () => {
  const mockProps = {
    onProfileClick: jest.fn(),
    onHomeClick: jest.fn()
  };

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  const renderHeader = async () => {
    let component;
    await act(async () => {
      component = render(
        <BrowserRouter>
          <AuthProvider>
            <Header {...mockProps} />
          </AuthProvider>
        </BrowserRouter>
      );
    });
    return component;
  };

  test('отображает логотип и кнопку главной', async () => {
    await renderHeader();
    expect(screen.getByAltText('Логотип')).toBeInTheDocument();
    expect(screen.getByText('Перейти на главную')).toBeInTheDocument();
  });

  test('отображает кнопки авторизации для неавторизованного пользователя', async () => {
    await renderHeader();
    expect(screen.getByText('Войти')).toBeInTheDocument();
    expect(screen.getByText('Регистрация')).toBeInTheDocument();
  });

  test('отображает имя пользователя для авторизованного пользователя', async () => {
    localStorage.setItem('user', JSON.stringify(mockUser));
    (userService.getCurrentUser as jest.Mock).mockResolvedValueOnce(mockUser);
    await renderHeader();
    
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0));
    });
    
    expect(screen.getByText(mockUser.username)).toBeInTheDocument();
  });

  test('обрабатывает клик по кнопке главной', async () => {
    await renderHeader();
    const homeButton = screen.getByText('Перейти на главную');
    await act(async () => {
      fireEvent.click(homeButton);
    });
    expect(mockProps.onHomeClick).toHaveBeenCalled();
  });

  test('обрабатывает выход из системы', async () => {
    localStorage.setItem('user', JSON.stringify(mockUser));
    (userService.getCurrentUser as jest.Mock).mockResolvedValueOnce(mockUser);
    await renderHeader();
    
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0));
    });
    
    const userDropdown = screen.getByText(mockUser.username);
    await act(async () => {
      fireEvent.click(userDropdown);
    });
    
    const logoutButton = screen.getByText('Выйти');
    await act(async () => {
      fireEvent.click(logoutButton);
    });
    
    expect(userService.logout).toHaveBeenCalled();
  });
}); 