import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../../../contexts/AuthContext';
import MainTab from '../MainTab';
import { act } from 'react';

const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  avatarUrl: '/test-avatar.jpg',
  status: 'active',
  isVerified: true,
  createdAt: new Date().toISOString()
};

const mockNavigate = jest.fn();

// Мокаем react-router
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

describe('MainTab Component', () => {
  const renderMainTab = async () => {
    let component;
    await act(async () => {
      component = render(
        <BrowserRouter>
          <AuthProvider>
            <MainTab isActive={true} />
          </AuthProvider>
        </BrowserRouter>
      );
    });
    return component;
  };

  beforeEach(() => {
    localStorage.clear();
    jest.clearAllMocks();
  });

  test('отображает заголовок вкладки', async () => {
    await renderMainTab();
    expect(screen.getByText('Главная')).toBeInTheDocument();
  });

  test('отображает содержимое вкладки', async () => {
    await renderMainTab();
    expect(screen.getByText('Содержимое вкладки моей страницы')).toBeInTheDocument();
  });

  test('применяет класс active когда isActive=true', async () => {
    await renderMainTab();
    expect(screen.getByRole('heading').closest('.tab')).toHaveClass('active');
  });
}); 