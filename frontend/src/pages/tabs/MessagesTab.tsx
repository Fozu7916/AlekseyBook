import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './Tabs.css';
import './MessagesTab.css';
import { TabProps } from './types';
import { userService, User, Message, ChatPreview } from '../../services/userService';
import { chatService } from '../../services/chatService';
import { logger } from '../../services/loggerService';
import { getMediaUrl } from '../../config/api.config';

const MessagesTab: React.FC<TabProps> = ({ isActive }) => {
  const [chats, setChats] = useState<ChatPreview[]>([]);
  const [selectedChat, setSelectedChat] = useState<User | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [isTyping, setIsTyping] = useState(false);
  const [typingUsers, setTypingUsers] = useState<{[key: string]: boolean}>({});
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined);
  const lastChatsUpdateRef = useRef<number>(0);
  const chatsCacheRef = useRef<{[key: string]: Message[]}>({});
  const loadChatsRef = useRef<((force: boolean) => Promise<void>) | null>(null);
  const { userId } = useParams<{ userId?: string }>();
  const navigate = useNavigate();
  const MESSAGES_PER_PAGE = 40;
  const CHATS_UPDATE_INTERVAL = 30000; // секунды
  const TYPING_TIMEOUT = 3000; // секунды

  const loadChats = useCallback(async (force: boolean = false) => {
    try {
      const token = localStorage.getItem('token');
      if (!token) {
        return;
      }

      const now = Date.now();
      if (!force && (now - lastChatsUpdateRef.current < CHATS_UPDATE_INTERVAL)) {
        return;
      }

      if (chats.length === 0) {
        setIsLoading(true);
      }
      
      const userChats = await userService.getUserChats();
      lastChatsUpdateRef.current = now;
      
      setChats(prev => {
        const hasChanges = userChats.length !== prev.length || 
          userChats.some((newChat, index) => {
            const oldChat = prev[index];
            return !oldChat || 
              newChat.user.id !== oldChat.user.id ||
              newChat.lastMessage?.id !== oldChat.lastMessage?.id ||
              newChat.unreadCount !== oldChat.unreadCount;
          });
        return hasChanges ? userChats : prev;
      });
    } catch (err) {
      logger.error('Ошибка при загрузке чатов', err);
    } finally {
      setIsLoading(false);
    }
  }, [chats.length]);

  loadChatsRef.current = loadChats;

  const markMessagesAsRead = useCallback(async (userId: number) => {
    try {
      await userService.markMessagesAsRead(userId);
      if (loadChatsRef.current) {
        loadChatsRef.current(false);
      }
    } catch (err) {
      logger.error('Ошибка при отметке сообщений как прочитанных', err);
    }
  }, []);

  const loadMessages = useCallback(async (userId: number, reset: boolean = false) => {
    try {
      const cacheKey = userId.toString();
      if (!reset && chatsCacheRef.current[cacheKey]) {
        setMessages(chatsCacheRef.current[cacheKey]);
        return;
      }

      setIsLoading(true);
      const chatMessages = await userService.getChatMessages(userId);
      
      const sortedMessages = [...chatMessages].sort((a, b) => 
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      ).map(msg => ({
        ...msg,
        createdAt: new Date(new Date(msg.createdAt).getTime() - new Date().getTimezoneOffset() * 60000).toISOString()
      }));

      chatsCacheRef.current[cacheKey] = sortedMessages;
      setMessages(sortedMessages);
      
      if (reset) {
        setTimeout(scrollToBottom, 100);
      }

      setHasMore(sortedMessages.length >= MESSAGES_PER_PAGE);
    } catch (err) {
      logger.error('Ошибка при загрузке сообщений', err);
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке сообщений');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (loadChatsRef.current) {
      loadChatsRef.current(true);
    }
  }, []);

  // Инициализация чата
  useEffect(() => {
    let isComponentMounted = true;
    let retryTimeout: NodeJS.Timeout;
    let isConnecting = false;

    const setupChat = async () => {
      if (isConnecting || !isComponentMounted) return;
      
      try {
        const token = localStorage.getItem('token');
        if (!token) {
          return;
        }

        if (chatService.isConnected()) {
          return;
        }

        isConnecting = true;

        await chatService.startConnection();
        
        if (!isComponentMounted) {
          await chatService.stopConnection();
          return;
        }
        
        if (chats.length === 0 && loadChatsRef.current) {
          await loadChatsRef.current(true);
        }
        
        if (userId && selectedChat?.id !== parseInt(userId)) {
          await loadUserAndChat(parseInt(userId));
        }

      } catch (err) {
        logger.error('Ошибка при инициализации чата', err);
        if (isComponentMounted) {
          setError('Ошибка подключения к чату. Пробуем переподключиться...');
          if (!retryTimeout) {
            retryTimeout = setTimeout(setupChat, 10000);
          }
        }
      } finally {
        isConnecting = false;
      }
    };

    if (isActive) {
      setupChat();
    }

    return () => {
      isComponentMounted = false;
      if (retryTimeout) {
        clearTimeout(retryTimeout);
      }
    };
  }, [isActive, userId, chats.length, selectedChat?.id]);

  useEffect(() => {
    if (selectedChat) {
      loadMessages(selectedChat.id, true);
      markMessagesAsRead(selectedChat.id);

      const unsubscribe = chatService.onTypingStatus((userId, isTyping) => {
        if (userId === selectedChat.id.toString()) {
          setTypingUsers(prev => ({ ...prev, [userId]: isTyping }));
        }
      });

      return () => {
        unsubscribe();
      };
    }
  }, [selectedChat, loadMessages, markMessagesAsRead]);

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
  }, [selectedChat, hasMore, isLoading, messages.length, loadMessages]);

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
      logger.error('Ошибка при загрузке пользователя', err);
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке пользователя');
    }
  };

  const formatMessageTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const hours = date.getHours();
    const minutes = date.getMinutes();
    
    return `${hours}:${minutes.toString().padStart(2, '0')}`;
  };

  const handleTyping = useCallback(async () => {
    if (!selectedChat) return;

    try {
      if (isTyping) {
        if (typingTimeoutRef.current) {
          clearTimeout(typingTimeoutRef.current);
        }
      } else {
        setIsTyping(true);
        if (chatService.isConnected()) {
          await chatService.sendTypingStatus(selectedChat.id.toString(), true);
        }
      }

      typingTimeoutRef.current = setTimeout(async () => {
        setIsTyping(false);
        if (chatService.isConnected()) {
          await chatService.sendTypingStatus(selectedChat.id.toString(), false);
        }
      }, TYPING_TIMEOUT);
    } catch (err) {
      logger.error('Ошибка при отправке статуса печатания', err);
    }
  }, [selectedChat, isTyping]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedChat || !newMessage.trim()) return;

    const wasTyping = isTyping;
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
      setIsTyping(false);
    }

    const messageContent = newMessage.trim();
    setNewMessage('');

    try {
      const currentUser = await userService.getCurrentUser();
      if (!currentUser) throw new Error('Пользователь не авторизован');

      await userService.sendMessage(selectedChat.id, messageContent);
      
      setTimeout(scrollToBottom, 100);

      if (wasTyping && chatService.isConnected()) {
        await chatService.sendTypingStatus(selectedChat.id.toString(), false);
      }
    } catch (error) {
      logger.error('Ошибка при отправке сообщения:', error);
      setNewMessage(messageContent);
    }
  };

  const handleChatSelect = useCallback(async (user: User) => {
    if (selectedChat?.id === user.id) {
      return;
    }

    setSelectedChat(user);
    setMessages([]);
    navigate(`/messages/${user.id}`);
    
    try {
      await loadMessages(user.id, true);
      await userService.markMessagesAsRead(user.id);
      
      const now = Date.now();
      if (now - lastChatsUpdateRef.current >= 5000 && loadChatsRef.current) {
        loadChatsRef.current(false);
      }
    } catch (err) {
      logger.error('Ошибка при загрузке чата', err);
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке чата');
    }
  }, [selectedChat, loadMessages, navigate]);

  useEffect(() => {
    if (userId && (!selectedChat || selectedChat.id !== parseInt(userId))) {
      loadUserAndChat(parseInt(userId));
    }
  }, [userId, selectedChat]);

  const scrollToBottom = () => {
    const container = messagesContainerRef.current;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  };

  useEffect(() => {
    if (!isActive || !chatService.isConnected()) {
      return;
    }
    
    const unsubscribeMessage = chatService.onMessage((message) => {
      const normalizedMessage = {
        ...message,
        createdAt: new Date(new Date(message.createdAt).getTime() - new Date().getTimezoneOffset() * 60000).toISOString()
      };

      setMessages(prev => {
        if (prev.some(m => m.id === message.id)) {
          return prev;
        }

        if (selectedChat && 
            (message.sender.id === selectedChat.id || 
             message.receiver.id === selectedChat.id)) {
          
          if (loadChatsRef.current) {
            loadChatsRef.current(true);
          }
          
          if (message.sender.id === selectedChat.id) {
            markMessagesAsRead(selectedChat.id);
          }
          
          setTimeout(scrollToBottom, 100);
          return [...prev, normalizedMessage];
        }
        
        return prev;
      });
    });

    const unsubscribeTyping = chatService.onTypingStatus((userId, isTyping) => {
      if (selectedChat && userId === selectedChat.id.toString()) {
        setTypingUsers(prev => {
          if (prev[userId] === isTyping) {
            return prev;
          }
          return { ...prev, [userId]: isTyping };
        });
      }
    });

    return () => {
      unsubscribeMessage();
      unsubscribeTyping();
    };
  }, [isActive, selectedChat, markMessagesAsRead]);

  const isConnected = chatService.isConnected();

  useEffect(() => {
    if (!isActive || !isConnected) return;

    const interval = setInterval(() => {
      if (loadChatsRef.current) {
        loadChatsRef.current(false);
      }
    }, CHATS_UPDATE_INTERVAL);

    return () => {
      clearInterval(interval);
    };
  }, [isActive, isConnected]);

  const handleMessageChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newText = e.target.value;
    setNewMessage(newText);
    
    if (newText.trim()) {
      handleTyping();
    } else if (isTyping) {
      setIsTyping(false);
      if (selectedChat && chatService.isConnected()) {
        chatService.sendTypingStatus(selectedChat.id.toString(), false)
          .catch(err => logger.error('Ошибка при отключении статуса печатания', err));
      }
    }
  };

  const renderTypingStatus = () => {
    if (!selectedChat) return null;
    
    const isUserTyping = typingUsers[selectedChat.id.toString()];
    
    if (isUserTyping) {
      return (
        <span className="typing-status">
          печатает
          <span className="typing-dots">
            <span className="typing-dot"></span>
            <span className="typing-dot"></span>
            <span className="typing-dot"></span>
          </span>
        </span>
      );
    }
    
    return selectedChat.status || 'Нет статуса';
  };

  if (!isActive) return null;

  return (
    <div className="messages-container">
      <div className="chats-list">
        <div className="chats-header">
          <h2>Сообщения</h2>
        </div>
        <div className="chats">
          {isLoading && chats.length === 0 && (
            <div data-testid="loading-indicator" className="loading">
              Загрузка...
            </div>
          )}
          {error && (
            <div data-testid="error-message" className="error">
              {error}
            </div>
          )}
          {chats.map(chat => (
            <div
              key={chat.user.id}
              className={`chat-item ${selectedChat?.id === chat.user.id ? 'active' : ''}`}
              onClick={() => handleChatSelect(chat.user)}
            >
              <img
                src={getMediaUrl(chat.user.avatarUrl)}
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
      </div>

      <div className="chat-content">
        {selectedChat ? (
          <>
            <div className="chat-header">
              <img
                src={getMediaUrl(selectedChat.avatarUrl)}
                alt={selectedChat.username}
                className="chat-avatar"
              />
              <div className="chat-info">
                <div className="chat-name">{selectedChat.username}</div>
                <div className="chat-status">
                  {renderTypingStatus()}
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
                onChange={handleMessageChange}
                placeholder="Введите сообщение..."
                onKeyDown={e => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    if (newMessage.trim()) {
                      handleSendMessage(e);
                    }
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