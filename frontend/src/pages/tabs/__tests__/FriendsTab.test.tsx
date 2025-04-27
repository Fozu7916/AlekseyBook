import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../../../contexts/AuthContext';
import FriendsTab from '../FriendsTab';
import { userService } from '../../../services/userService';
import { act } from 'react';

const mockNavigate = jest.fn();

// Мокаем react-router
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

jest.mock('../../../services/userService', () => ({
  userService: {
    getFriendsList: jest.fn(),
    acceptFriendRequest: jest.fn(),
    declineFriendRequest: jest.fn(),
    removeFriend: jest.fn()
  }
}));

const mockFriends = [
  {
    id: 1,
    username: 'friend1',
    email: 'friend1@test.com',
    avatarUrl: '/friend1-avatar.jpg',
    status: 'active',
    isVerified: true,
    createdAt: new Date().toISOString()
  }
];

const mockPendingRequests = [
  {
    id: 2,
    username: 'pending1',
    email: 'pending1@test.com',
    avatarUrl: '/pending1-avatar.jpg',
    status: 'active',
    isVerified: true,
    createdAt: new Date().toISOString()
  }
];

describe('FriendsTab Component', () => {
  const renderFriendsTab = async () => {
    let component;
    await act(async () => {
      component = render(
        <BrowserRouter>
          <AuthProvider>
            <FriendsTab isActive={true} />
          </AuthProvider>
        </BrowserRouter>
      );
    });
    return component;
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (userService.getFriendsList as jest.Mock).mockResolvedValue({
      friends: mockFriends,
      pendingRequests: mockPendingRequests,
      sentRequests: []
    });
  });

  test('отображает список друзей', async () => {
    await renderFriendsTab();
    expect(screen.getByText('friend1')).toBeInTheDocument();
  });

  test('отображает кнопки действий для друзей', async () => {
    await renderFriendsTab();
    expect(screen.getByText('Сообщение')).toBeInTheDocument();
    expect(screen.getByText('Удалить из друзей')).toBeInTheDocument();
  });

  test('обрабатывает удаление друга', async () => {
    await renderFriendsTab();
    const removeButton = screen.getByText('Удалить из друзей');
    await act(async () => {
      fireEvent.click(removeButton);
    });
    expect(userService.removeFriend).toHaveBeenCalledWith(1);
  });

  test('отображает входящие заявки', async () => {
    await renderFriendsTab();
    const pendingTab = screen.getByText(/входящие/i);
    await act(async () => {
      fireEvent.click(pendingTab);
    });
    expect(screen.getByText('pending1')).toBeInTheDocument();
  });

  test('обрабатывает принятие заявки в друзья', async () => {
    await renderFriendsTab();
    const pendingTab = screen.getByText(/входящие/i);
    await act(async () => {
      fireEvent.click(pendingTab);
    });
    const acceptButton = screen.getByText('Принять');
    await act(async () => {
      fireEvent.click(acceptButton);
    });
    expect(userService.acceptFriendRequest).toHaveBeenCalledWith(2);
  });
}); 