import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import MessagesTab from '../MessagesTab';
import { chatService } from '../../../services/chatService';
import { userService } from '../../../services/userService';

// Мокаем сервисы
jest.mock('../../../services/chatService', () => ({
  chatService: {
    isConnected: jest.fn(() => true),
    startConnection: jest.fn(),
    stopConnection: jest.fn(),
    onTypingStatus: jest.fn(() => () => {}),
    onMessage: jest.fn(() => () => {}),
    sendMessage: jest.fn(),
    sendTypingStatus: jest.fn(),
  }
}));

jest.mock('../../../services/userService', () => ({
  userService: {
    getUserChats: jest.fn(() => Promise.resolve([])),
    getChatMessages: jest.fn(() => Promise.resolve([])),
    getUserById: jest.fn(() => Promise.resolve({ id: 1, username: 'testUser' })),
    markMessagesAsRead: jest.fn(() => Promise.resolve()),
    sendMessage: jest.fn(() => Promise.resolve({ 
      id: 1, 
      content: 'Test message',
      createdAt: new Date().toISOString()
    })),
    getCurrentUser: jest.fn(() => Promise.resolve({ id: 1, username: 'testUser' })),
  }
}));

// Мокаем react-router-dom
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => jest.fn(),
  useParams: () => ({ userId: undefined }),
}));

const mockChats = [
  {
    user: { id: 1, username: 'user1', avatar: null },
    lastMessage: { id: 1, content: 'Hello', createdAt: new Date().toISOString() },
    unreadCount: 0
  },
  {
    user: { id: 2, username: 'user2', avatar: null },
    lastMessage: { id: 2, content: 'Hi', createdAt: new Date().toISOString() },
    unreadCount: 1
  }
];

const mockMessages = [
  { id: 1, content: 'Hello', sender: { id: 1, username: 'user1' }, createdAt: new Date().toISOString() },
  { id: 2, content: 'Hi there', sender: { id: 2, username: 'user2' }, createdAt: new Date().toISOString() }
];

describe('MessagesTab', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.setItem('token', 'test-token');
    localStorage.setItem('userId', '1');

    // Базовые моки
    (userService.getUserChats as jest.Mock).mockResolvedValue(mockChats);
    (userService.getChatMessages as jest.Mock).mockResolvedValue(mockMessages);
    (userService.getUserById as jest.Mock).mockResolvedValue({ id: 1, username: 'user1' });
    (userService.sendMessage as jest.Mock).mockResolvedValue({ id: 3, content: 'New message' });
    
    (chatService.isConnected as jest.Mock).mockReturnValue(true);
    (chatService.startConnection as jest.Mock).mockResolvedValue(undefined);
    (chatService.onTypingStatus as jest.Mock).mockImplementation(() => () => {});
    (chatService.onMessage as jest.Mock).mockImplementation(() => () => {});
  });

  const renderComponent = async (isActive = true, userId?: string) => {
    let result;
    await act(async () => {
      result = render(
        <MemoryRouter initialEntries={[userId ? `/messages/${userId}` : '/messages']}>
          <Routes>
            <Route path="/messages" element={<MessagesTab isActive={isActive} />} />
            <Route path="/messages/:userId" element={<MessagesTab isActive={isActive} />} />
          </Routes>
        </MemoryRouter>
      );
    });
    return result;
  };

  test('загружает и отображает список чатов', async () => {
    await renderComponent();
    
    await waitFor(() => {
      expect(screen.getByText('user1')).toBeInTheDocument();
      expect(screen.getByText('user2')).toBeInTheDocument();
    });
  });

  test('отображает сообщение об ошибке при неудачной загрузке чатов', async () => {
    (userService.getUserChats as jest.Mock).mockRejectedValue(new Error('Ошибка при загрузке чатов'));
    await renderComponent();
    
    await waitFor(() => {
      expect(screen.getByText('Выберите чат для начала общения')).toBeInTheDocument();
    });
  });

  test('отправляет сообщение при отправке формы', async () => {
    // Мокаем текущего пользователя
    (userService.getCurrentUser as jest.Mock).mockResolvedValue({ 
      id: 2, 
      username: 'currentUser' 
    });

    await renderComponent(true, '1');
    
    // Сначала кликаем по чату чтобы открыть его
    const chatItem = await screen.findByText('user1');
    await act(async () => {
      fireEvent.click(chatItem);
    });

    // Теперь ищем форму и отправляем сообщение
    const textarea = screen.getByPlaceholderText('Введите сообщение...');
    await act(async () => {
      fireEvent.change(textarea, { target: { value: 'New message' } });
    });
    
    const submitButton = screen.getByText('Отправить');
    const form = submitButton.closest('form');
    if (!form) throw new Error('Form not found');
    
    await act(async () => {
      fireEvent.submit(form);
    });
    
    await waitFor(() => {
      expect(userService.sendMessage).toHaveBeenCalledWith(1, 'New message');
    });
  });

  test('отображает статус печатания', async () => {
    let typingCallback: (userId: string, isTyping: boolean) => void;
    (chatService.onTypingStatus as jest.Mock).mockImplementation((callback) => {
      typingCallback = callback;
      return () => {};
    });

    await renderComponent(true, '1');
    
    // Сначала кликаем по чату чтобы открыть его
    const chatItem = await screen.findByText('user1');
    await act(async () => {
      fireEvent.click(chatItem);
    });

    await act(async () => {
      typingCallback('1', true);
    });

    await waitFor(() => {
      const typingStatus = screen.getByText('печатает');
      expect(typingStatus).toBeInTheDocument();
    });
  });
}); 