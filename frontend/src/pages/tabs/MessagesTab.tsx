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
      if (!force && (now - lastChatsUpdateRef.current < CHATS_UPDATE_INTERVAL || isLoading)) {
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
  }, [chats.length, isLoading]);

  // Инициализация чата
  useEffect(() => {
    if (isActive) {
      let isComponentMounted = true;
      let retryTimeout: NodeJS.Timeout;

      const setupChat = async () => {
        try {
          if (chatService.isConnected()) {
            return;
          }

          await chatService.startConnection();
           
          if (!isComponentMounted) return;

          await loadChats(true);
           
          if (userId && isComponentMounted) {
            await loadUserAndChat(parseInt(userId));
          }

          // Подписываемся на события после успешного подключения
          chatService.onMessage((message) => {
            if (!isComponentMounted) return;
            console.log('Получено новое сообщение:', message);

            // Обновляем список чатов при любом новом сообщении
            loadChats(true);

            // Если сообщение относится к текущему чату, добавляем его в список
            if (selectedChat && 
                (message.sender.id === selectedChat.id || 
                 message.receiver.id === selectedChat.id)) {
              const localMessage = {
                ...message,
                createdAt: new Date(new Date(message.createdAt).getTime() - new Date().getTimezoneOffset() * 60000).toISOString()
              };
              
              setMessages(prev => {
                // Проверяем, нет ли уже такого сообщения
                const messageExists = prev.some(m => m.id === message.id);
                if (messageExists) {
                  return prev;
                }
                return [...prev, localMessage];
              });

              setTimeout(scrollToBottom, 100);
              
              // Отмечаем сообщения как прочитанные, если они от текущего собеседника
              if (message.sender.id === selectedChat.id) {
                markMessagesAsRead(selectedChat.id);
              }

              // Сбрасываем статус печатания при получении сообщения от собеседника
              if (message.sender.id === selectedChat.id) {
                setTypingUsers(prev => ({ ...prev, [selectedChat.id.toString()]: false }));
              }
            }
          });

          // Подписываемся на события печатания
          chatService.onTypingStatus((userId, isTyping) => {
            if (!isComponentMounted) return;
            console.log('Получен статус печатания:', userId, isTyping);
            setTypingUsers(prev => ({ ...prev, [userId]: isTyping }));
          });

        } catch (err) {
          console.error('Ошибка при инициализации чата:', err);
          if (isComponentMounted) {
            setError('Ошибка подключения к чату. Пробуем переподключиться...');
            retryTimeout = setTimeout(setupChat, 5000);
          }
        }
      };

      setupChat();

      return () => {
        isComponentMounted = false;
        if (retryTimeout) {
          clearTimeout(retryTimeout);
        }
        // Сбрасываем все состояния печатания при размонтировании
        setTypingUsers({});
        chatService.stopConnection();
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
      if (container.scrollTop < 100 && hasMore && !isLoading && messages.length >= MESSAGES_PER_PAGE) {
        loadMessages(selectedChat!.id, false);
      }
    }
  }, [selectedChat, hasMore, isLoading, messages.length]);

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
      // Если сообщения уже загружены и это не принудительная перезагрузка, не делаем запрос
      if (!reset && messages.length > 0) {
        return;
      }

      setIsLoading(true);
      
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

      // Устанавливаем hasMore только если сообщений больше или равно MESSAGES_PER_PAGE
      setHasMore(messagesWithLocalTime.length >= MESSAGES_PER_PAGE);
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
    
    setIsTyping(false);
    if (chatService.isConnected()) {
      chatService.sendTypingStatus(selectedChat.id.toString(), false);
    }

    const messageContent = newMessage.trim();
    setNewMessage('');

    // Создаем временное сообщение за пределами try-catch
    let tempMessageId = Date.now();

    try {
      const currentUser = await userService.getCurrentUser();
      if (!currentUser) throw new Error('Пользователь не авторизован');

      // Создаем временное сообщение
      const tempMessage = {
        id: tempMessageId,
        content: messageContent,
        sender: currentUser,
        receiver: selectedChat,
        isRead: false,
        createdAt: new Date().toISOString()
      };

      // Показываем сообщение сразу
      setMessages(prev => [...prev, tempMessage]);
      setTimeout(scrollToBottom, 100);

      // Отправляем сообщение через HTTP
      const sentMessage = await userService.sendMessage(selectedChat.id, messageContent);

      // Пытаемся отправить через SignalR только если есть подключение
      if (chatService.isConnected()) {
        try {
          await chatService.sendMessage(sentMessage); // Отправляем реальное сообщение вместо временного
        } catch (err) {
          console.error('Ошибка отправки через SignalR:', err);
        }
      }

      // Заменяем временное сообщение на реальное
      setMessages(prev => 
        prev.map(msg => 
          msg.id === tempMessageId ? sentMessage : msg
        )
      );

      // Обновляем список чатов после отправки
      loadChats(true);

    } catch (err) {
      console.error('Ошибка при отправке сообщения:', err);
      setError(err instanceof Error ? err.message : 'Ошибка при отправке сообщения');
      // Удаляем временное сообщение в случае ошибки
      setMessages(prev => prev.filter(msg => msg.id !== tempMessageId));
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
    
    if (selectedChat && !isTyping && chatService.isConnected()) {
      setIsTyping(true);
      chatService.sendTypingStatus(selectedChat.id.toString(), true);
    }

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      if (selectedChat && chatService.isConnected()) {
        chatService.sendTypingStatus(selectedChat.id.toString(), false);
      }
    }, 3000);
  }, [selectedChat, isTyping]);

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