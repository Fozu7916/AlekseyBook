import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './Tabs.css';
import './MessagesTab.css';
import { TabProps } from './types';
import { userService, User, Message, ChatPreview } from '../../services/userService';
import { chatService } from '../../services/chatService';

const MessagesTab: React.FC<TabProps> = ({ isActive }) => {
  const [chats, setChats] = useState<ChatPreview[]>([]);
  const [selectedChat, setSelectedChat] = useState<User | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [isTyping, setIsTyping] = useState(false);
  const [typingUsers, setTypingUsers] = useState<{[key: string]: boolean}>({});
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined);
  const lastChatsUpdateRef = useRef<number>(0);
  const { userId } = useParams<{ userId?: string }>();
  const navigate = useNavigate();
  const MESSAGES_PER_PAGE = 40;
  const CHATS_UPDATE_INTERVAL = 3000; // 3 секунды

  const loadChats = useCallback(async (force: boolean = false) => {
    try {
      // Проверяем, прошло ли достаточно времени с последнего обновления
      const now = Date.now();
      if (!force && now - lastChatsUpdateRef.current < CHATS_UPDATE_INTERVAL) {
        return;
      }

      // Не показываем состояние загрузки, если уже есть чаты
      if (chats.length === 0) {
        setIsLoading(true);
      }
      
      const userChats = await userService.getUserChats();
      lastChatsUpdateRef.current = now;
      
      // Сравниваем новые чаты с текущими, обновляем только при изменениях
      setChats(prev => {
        const hasChanges = JSON.stringify(prev) !== JSON.stringify(userChats);
        return hasChanges ? userChats : prev;
      });
    } catch (err) {
      console.error('Ошибка при загрузке чатов:', err);
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке чатов');
    } finally {
      setIsLoading(false);
    }
  }, [chats.length]);

  // Инициализация чата
  useEffect(() => {
    if (isActive) {
      const setupChat = async () => {
        try {
          await chatService.startConnection();
          await loadChats(true); // Принудительная первая загрузка
          if (userId) {
            await loadUserAndChat(parseInt(userId));
          }
        } catch (err) {
          console.error('Ошибка при инициализации чата:', err);
          setError('Ошибка подключения к чату. Пробуем переподключиться...');
        }
      };

      setupChat();

      const connectionCheckInterval = setInterval(() => {
        if (!chatService.isConnected()) {
          console.log('Проверка подключения: переподключаемся...');
          setupChat();
        }
      }, 5000);

      // Подписываемся на новые сообщения
      const unsubscribeMessage = chatService.onMessage((message) => {
        console.log('Получено новое сообщение в компоненте:', message);
        
        // Обновляем список сообщений, если сообщение относится к текущему чату
        if (selectedChat && 
            (message.sender.id === selectedChat.id || 
             message.receiver.id === selectedChat.id)) {
          updateMessages();
        }
        
        // Обновляем список чатов с небольшой задержкой
        setTimeout(() => loadChats(true), 500);
      });

      // Периодическое обновление списка чатов
      const chatUpdateInterval = setInterval(() => loadChats(false), CHATS_UPDATE_INTERVAL);

      return () => {
        clearInterval(connectionCheckInterval);
        clearInterval(chatUpdateInterval);
        chatService.stopConnection();
        unsubscribeMessage();
      };
    }
  }, [isActive, selectedChat, loadChats, userId]);

  useEffect(() => {
    if (selectedChat) {
      console.log('Выбран новый чат:', selectedChat.id);
      loadMessages(selectedChat.id, true);
      markMessagesAsRead(selectedChat.id);

      // Подписываемся на события печатания
      const unsubscribe = chatService.onTypingStatus((userId, isTyping) => {
        console.log('Получен статус печатания:', userId, isTyping, 'текущий чат:', selectedChat.id);
        if (userId === selectedChat.id.toString()) {
          setTypingUsers(prev => ({ ...prev, [userId]: isTyping }));
        }
      });

      return () => {
        unsubscribe();
      };
    }
  }, [selectedChat]);

  useEffect(() => {
    if (messages.length > 0) {
      scrollToBottom();
    }
  }, [selectedChat, messages]);

  const handleScroll = useCallback(() => {
    const container = messagesContainerRef.current;
    if (container) {
      if (container.scrollTop < 100 && hasMore && !isLoading) {
        loadMessages(selectedChat!.id, false);
      }
    }
  }, [selectedChat, hasMore, isLoading]);

  useEffect(() => {
    const container = messagesContainerRef.current;
    if (container) {
      container.addEventListener('scroll', handleScroll);
      return () => container.removeEventListener('scroll', handleScroll);
    }
  }, [handleScroll]);

  const loadUserAndChat = async (userId: number) => {
    try {
      const user = await userService.getUserById(userId);
      setSelectedChat(user);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке пользователя');
    }
  };

  const loadMessages = async (userId: number, reset: boolean) => {
    try {
      setIsLoading(true);
      
      if (reset) {
        setPage(1);
        setHasMore(true);
      }

      const chatMessages = await userService.getChatMessages(userId);
      
      // Сортируем сообщения по времени
      const sortedMessages = [...chatMessages].sort((a, b) => 
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      );
      
      // Преобразуем время в локальное
      const messagesWithLocalTime = sortedMessages.map(msg => ({
        ...msg,
        createdAt: new Date(new Date(msg.createdAt).getTime() - new Date().getTimezoneOffset() * 60000).toISOString()
      }));

      setMessages(messagesWithLocalTime);
      
      if (reset) {
        setTimeout(scrollToBottom, 100);
      }

      setHasMore(false); // Временно отключаем пагинацию
    } catch (err) {
      console.error('Ошибка при загрузке сообщений:', err);
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке сообщений');
    } finally {
      setIsLoading(false);
    }
  };

  const markMessagesAsRead = async (userId: number) => {
    try {
      await userService.markMessagesAsRead(userId);
      loadChats();
    } catch (err) {
      console.error('Ошибка при отметке сообщений как прочитанных:', err);
    }
  };

  const formatMessageTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const hours = date.getHours();
    const minutes = date.getMinutes();
    
    return `${hours}:${minutes.toString().padStart(2, '0')}`;
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedChat || !newMessage.trim()) return;

    // Мгновенно отключаем статус печатания при отправке
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }
    if (selectedChat && isTyping) {
      setIsTyping(false);
      chatService.sendTypingStatus(selectedChat.id.toString(), false);
    }

    const messageContent = newMessage.trim();
    setNewMessage('');

    try {
      const currentUser = await userService.getCurrentUser();
      if (!currentUser) throw new Error('Пользователь не авторизован');

      // Создаем временное сообщение
      const tempMessage = {
        id: Date.now(),
        content: messageContent,
        sender: currentUser,
        receiver: selectedChat,
        isRead: false,
        createdAt: new Date().toISOString()
      };

      // Показываем сообщение сразу
      setMessages(prev => [...prev, tempMessage]);
      setTimeout(scrollToBottom, 100);

      // Отправляем сообщение через HTTP и SignalR
      const [sentMessage] = await Promise.all([
        userService.sendMessage(selectedChat.id, messageContent),
        chatService.sendMessage(tempMessage).catch(err => {
          console.error('Ошибка отправки через SignalR:', err);
          return null;
        })
      ]);

      // Заменяем временное сообщение на реальное только если получили ответ от сервера
      if (sentMessage) {
        setMessages(prev => 
          prev.map(msg => 
            msg.id === tempMessage.id ? sentMessage : msg
          )
        );
      }

    } catch (err) {
      console.error('Ошибка при отправке сообщения:', err);
      setError(err instanceof Error ? err.message : 'Ошибка при отправке сообщения');
      // Удаляем временное сообщение в случае ошибки
      setMessages(prev => prev.filter(msg => msg.id !== Date.now()));
    }
  };

  const handleChatSelect = async (user: User) => {
    setSelectedChat(user);
    setMessages([]); // Очищаем сообщения перед загрузкой новых
    navigate(`/messages/${user.id}`);
  };

  const scrollToBottom = () => {
    const container = messagesContainerRef.current;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  };

  const handleTyping = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setNewMessage(e.target.value);
    
    if (selectedChat && !isTyping) {
      setIsTyping(true);
      chatService.sendTypingStatus(selectedChat.id.toString(), true);
    }

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      if (selectedChat) {
        chatService.sendTypingStatus(selectedChat.id.toString(), false);
      }
    }, 3000);
  }, [selectedChat, isTyping]);

  // Функция для обновления списка сообщений
  const updateMessages = useCallback(async () => {
    if (selectedChat) {
      await loadMessages(selectedChat.id, false);
    }
  }, [selectedChat]);

  // Автоматическое обновление сообщений каждые 10 секунд
  useEffect(() => {
    if (isActive && selectedChat) {
      const interval = setInterval(updateMessages, 10000);
      return () => clearInterval(interval);
    }
  }, [isActive, selectedChat, updateMessages]);

  if (!isActive) return null;

  return (
    <div className="messages-container">
      <div className="chats-list">
        <div className="chats-header">
          <h2>Сообщения</h2>
        </div>
        {isLoading && chats.length === 0 ? (
          <div className="loading-message">Загрузка чатов...</div>
        ) : (
          <div className="chats">
            {chats.map(chat => (
              <div
                key={chat.user.id}
                className={`chat-item ${selectedChat?.id === chat.user.id ? 'active' : ''}`}
                onClick={() => handleChatSelect(chat.user)}
              >
                <img
                  src={chat.user.avatarUrl ? `http://localhost:5038${chat.user.avatarUrl}` : '/images/default-avatar.svg'}
                  alt={chat.user.username}
                  className="chat-avatar"
                />
                <div className="chat-info">
                  <div className="chat-name">{chat.user.username}</div>
                  <div className="chat-last-message">
                    {chat.lastMessage?.content || 'Нет сообщений'}
                  </div>
                </div>
                {chat.unreadCount > 0 && (
                  <div className="unread-count">{chat.unreadCount}</div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="chat-content">
        {selectedChat ? (
          <>
            <div className="chat-header">
              <img
                src={selectedChat.avatarUrl ? `http://localhost:5038${selectedChat.avatarUrl}` : '/images/default-avatar.svg'}
                alt={selectedChat.username}
                className="chat-avatar"
              />
              <div className="chat-info">
                <div className="chat-name">{selectedChat.username}</div>
                <div className="chat-status">
                  {typingUsers[selectedChat.id.toString()] ? (
                    <span className="typing-status">
                      печатает
                      <span className="typing-dots">
                        <span className="typing-dot"></span>
                        <span className="typing-dot"></span>
                        <span className="typing-dot"></span>
                      </span>
                    </span>
                  ) : (
                    selectedChat.status || 'Нет статуса'
                  )}
                </div>
              </div>
            </div>

            <div className="messages-list" ref={messagesContainerRef} onScroll={handleScroll}>
              {isLoading && hasMore && (
                <div className="load-more">
                  Загрузка сообщений...
                </div>
              )}
              {messages.map(message => (
                <div
                  key={message.id}
                  className={`message ${message.sender.id === selectedChat.id ? 'received' : 'sent'}`}
                >
                  <div className="message-content">{message.content}</div>
                  <div className="message-time">
                    {formatMessageTime(message.createdAt)}
                  </div>
                </div>
              ))}
            </div>

            <form className="message-input" onSubmit={handleSendMessage}>
              <textarea
                value={newMessage}
                onChange={handleTyping}
                placeholder="Введите сообщение..."
                onKeyDown={e => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendMessage(e);
                  }
                }}
              />
              <button type="submit" disabled={!newMessage.trim()}>
                Отправить
              </button>
            </form>
          </>
        ) : (
          <div className="no-chat-selected">
            Выберите чат для начала общения
          </div>
        )}
      </div>
    </div>
  );
};

export default MessagesTab; 