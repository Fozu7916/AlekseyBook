import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './Tabs.css';
import './MessagesTab.css';
import { TabProps } from './types';
import { userService, User, Message, ChatPreview } from '../../services/userService';

const MessagesTab: React.FC<TabProps> = ({ isActive }) => {
  const [chats, setChats] = useState<ChatPreview[]>([]);
  const [selectedChat, setSelectedChat] = useState<User | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { userId } = useParams<{ userId?: string }>();
  const navigate = useNavigate();

  useEffect(() => {
    if (isActive) {
      loadChats();
      if (userId) {
        loadUserAndChat(parseInt(userId));
      }
    }
  }, [isActive, userId]);

  useEffect(() => {
    if (selectedChat) {
      loadMessages(selectedChat.id);
      markMessagesAsRead(selectedChat.id);
    }
  }, [selectedChat]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const loadChats = async () => {
    try {
      setIsLoading(true);
      const userChats = await userService.getUserChats();
      setChats(userChats);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке чатов');
    } finally {
      setIsLoading(false);
    }
  };

  const loadUserAndChat = async (userId: number) => {
    try {
      const user = await userService.getUserById(userId);
      setSelectedChat(user);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке пользователя');
    }
  };

  const loadMessages = async (userId: number) => {
    try {
      const chatMessages = await userService.getChatMessages(userId);
      setMessages(chatMessages.reverse());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке сообщений');
    }
  };

  const markMessagesAsRead = async (userId: number) => {
    try {
      await userService.markMessagesAsRead(userId);
      // Обновляем список чатов, чтобы обновить счетчики непрочитанных сообщений
      loadChats();
    } catch (err) {
      console.error('Ошибка при отметке сообщений как прочитанных:', err);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedChat || !newMessage.trim()) return;

    try {
      const message = await userService.sendMessage(selectedChat.id, newMessage.trim());
      setMessages(prev => [...prev, message]);
      setNewMessage('');
      // Обновляем список чатов, чтобы обновить последнее сообщение
      loadChats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отправке сообщения');
    }
  };

  const handleChatSelect = (user: User) => {
    setSelectedChat(user);
    navigate(`/messages/${user.id}`);
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  if (!isActive) return null;

  return (
    <div className="messages-container">
      <div className="chats-list">
        <div className="chats-header">
          <h2>Сообщения</h2>
        </div>
        {isLoading ? (
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
                <div className="chat-status">{selectedChat.status || 'Нет статуса'}</div>
              </div>
            </div>

            <div className="messages-list">
              {messages.map(message => (
                <div
                  key={message.id}
                  className={`message ${message.sender.id === selectedChat.id ? 'received' : 'sent'}`}
                >
                  <div className="message-content">{message.content}</div>
                  <div className="message-time">
                    {new Date(message.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>
              ))}
              <div ref={messagesEndRef} />
            </div>

            <form className="message-input" onSubmit={handleSendMessage}>
              <textarea
                value={newMessage}
                onChange={e => setNewMessage(e.target.value)}
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