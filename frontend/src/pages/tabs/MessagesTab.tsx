import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './Tabs.css';
import './MessagesTab.css';
import { TabProps } from './types';
import { messageService, Chat, Message } from '../../services/messageService';
import { useAuth } from '../../contexts/AuthContext';
import { userService } from '../../services/userService';

interface MessagesTabProps extends TabProps {
  userId?: string;
}

const MessagesTab: React.FC<MessagesTabProps> = ({ isActive }) => {
  const { userId } = useParams<{ userId?: string }>();
  const [chats, setChats] = useState<Chat[]>([]);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [selectedChat, setSelectedChat] = useState<Chat | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { user } = useAuth();
  const navigate = useNavigate();

  // Загрузка списка чатов
  useEffect(() => {
    const fetchChats = async () => {
      try {
        setIsLoading(true);
        const chatsList = await messageService.getChats();
        setChats(chatsList);
        
        // Если есть userId в параметрах, создаем или находим соответствующий чат
        if (userId) {
          const existingChat = chatsList.find(c => c.userId === parseInt(userId));
          if (existingChat) {
            setSelectedChat(existingChat);
          } else {
            // Если чат не существует, получаем информацию о пользователе и создаем новый чат
            try {
              const userInfo = await userService.getUserById(parseInt(userId));
              const newChat: Chat = {
                userId: userInfo.id,
                username: userInfo.username,
                avatarUrl: userInfo.avatarUrl,
                unreadCount: 0
              };
              setChats(prev => [...prev, newChat]);
              setSelectedChat(newChat);
            } catch (err) {
              setError('Пользователь не найден');
            }
          }
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке чатов');
      } finally {
        setIsLoading(false);
      }
    };

    if (isActive) {
      fetchChats();
    }
  }, [isActive, userId]);

  // Загрузка сообщений выбранного чата
  useEffect(() => {
    const fetchMessages = async () => {
      if (!selectedChat) return;

      try {
        const messagesList = await messageService.getMessages(selectedChat.userId);
        setMessages(messagesList);
        // Помечаем сообщения как прочитанные
        await messageService.markAsRead(selectedChat.userId);
        // Обновляем количество непрочитанных в списке чатов
        setChats(prev => prev.map(chat => 
          chat.userId === selectedChat.userId 
            ? { ...chat, unreadCount: 0 }
            : chat
        ));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке сообщений');
      }
    };

    if (selectedChat) {
      fetchMessages();
    }
  }, [selectedChat]);

  // Прокрутка к последнему сообщению
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedChat || !newMessage.trim()) return;

    try {
      const message = await messageService.sendMessage(selectedChat.userId, newMessage.trim());
      setMessages(prev => [...prev, message]);
      setNewMessage('');
      
      // Обновляем последнее сообщение в списке чатов
      setChats(prev => prev.map(chat => 
        chat.userId === selectedChat.userId 
          ? { ...chat, lastMessage: message }
          : chat
      ));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отправке сообщения');
    }
  };

  const handleChatSelect = (chat: Chat) => {
    setSelectedChat(chat);
    setError(null);
    navigate(`/messages/${chat.userId}`);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === now.toDateString()) {
      return date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'вчера';
    } else {
      return date.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' });
    }
  };

  if (!isActive) return null;

  return (
    <div className="tab active">
      <div className="messages-container">
        <div className="chats-list">
          <div className="chats-header">
            <h2>Сообщения</h2>
          </div>
          {isLoading ? (
            <div className="loading-message">Загрузка...</div>
          ) : (
            <div className="chats">
              {chats.map(chat => (
                <div 
                  key={chat.userId}
                  className={`chat-item ${selectedChat?.userId === chat.userId ? 'active' : ''}`}
                  onClick={() => handleChatSelect(chat)}
                >
                  <img 
                    src={chat.avatarUrl ? `http://localhost:5038${chat.avatarUrl}` : '/images/default-avatar.svg'} 
                    alt={chat.username} 
                    className="chat-avatar"
                  />
                  <div className="chat-info">
                    <div className="chat-name">{chat.username}</div>
                    {chat.lastMessage && (
                      <div className="chat-last-message">
                        {chat.lastMessage.senderId === user?.id ? 'Вы: ' : ''}
                        {chat.lastMessage.content}
                      </div>
                    )}
                  </div>
                  {chat.unreadCount > 0 && (
                    <div className="unread-count">{chat.unreadCount}</div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="messages-content">
          {selectedChat ? (
            <>
              <div className="messages-header">
                <img 
                  src={selectedChat.avatarUrl ? `http://localhost:5038${selectedChat.avatarUrl}` : '/images/default-avatar.svg'} 
                  alt={selectedChat.username} 
                  className="user-avatar"
                />
                <div className="user-info">
                  <div className="user-name">{selectedChat.username}</div>
                </div>
              </div>

              <div className="messages-list">
                {messages.map(message => (
                  <div 
                    key={message.id} 
                    className={`message ${message.senderId === user?.id ? 'own' : ''}`}
                  >
                    <div className="message-content">
                      {message.content}
                      <span className="message-time">{formatDate(message.createdAt)}</span>
                    </div>
                  </div>
                ))}
                <div ref={messagesEndRef} />
              </div>

              <form className="message-input" onSubmit={handleSendMessage}>
                <textarea
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  placeholder="Введите сообщение..."
                  onKeyDown={(e) => {
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
              <p>Выберите чат для начала общения</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default MessagesTab; 