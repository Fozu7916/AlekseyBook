import React, { useState, useEffect, useMemo, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import './Tabs.css';
import './FriendsTab.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import { getMediaUrl } from '../../config/api.config';
import { notificationService } from '../../services/notificationService';
import { NOTIFICATION_TYPES } from '../../types/notification';

const FriendsTab: React.FC<TabProps> = ({ isActive }) => {
  const [friends, setFriends] = useState<User[]>([]);
  const [pendingRequests, setPendingRequests] = useState<User[]>([]);
  const [sentRequests, setSentRequests] = useState<User[]>([]);
  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<'friends' | 'pending' | 'sent' | 'all'>('friends');
  const [searchQuery, setSearchQuery] = useState('');
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [currentUserId, setCurrentUserId] = useState<number | null>(null);
  
  const observer = useRef<IntersectionObserver | null>(null);
  const lastUserElementRef = useCallback((node: HTMLDivElement | null) => {
    if (isLoadingMore) return;
    if (observer.current) observer.current.disconnect();
    observer.current = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting && hasMore) {
        setPage(prevPage => prevPage + 1);
      }
    });
    if (node) observer.current.observe(node);
  }, [isLoadingMore, hasMore]);

  const navigate = useNavigate();

  // Фильтрация списков на основе поискового запроса
  const filteredFriends = useMemo(() => {
    const query = searchQuery.toLowerCase();
    return friends.filter(friend => 
      friend.username.toLowerCase().includes(query)
    );
  }, [friends, searchQuery]);

  const filteredPendingRequests = useMemo(() => {
    const query = searchQuery.toLowerCase();
    return pendingRequests.filter(user => 
      user.username.toLowerCase().includes(query)
    );
  }, [pendingRequests, searchQuery]);

  const filteredSentRequests = useMemo(() => {
    const query = searchQuery.toLowerCase();
    return sentRequests.filter(user => 
      user.username.toLowerCase().includes(query)
    );
  }, [sentRequests, searchQuery]);

  const filteredAllUsers = useMemo(() => {
    const query = searchQuery.toLowerCase();
    return allUsers.filter(user => 
      user.username.toLowerCase().includes(query)
    );
  }, [allUsers, searchQuery]);

  useEffect(() => {
    // Получаем текущего пользователя из localStorage
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    if (user.id) {
      setCurrentUserId(user.id);
    }
  }, []);

  useEffect(() => {
    const fetchFriends = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const friendsList = await userService.getFriendsList();
        setFriends(friendsList.friends);
        setPendingRequests(friendsList.pendingRequests);
        setSentRequests(friendsList.sentRequests);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке списка друзей');
      } finally {
        setIsLoading(false);
      }
    };

    if (isActive) {
      fetchFriends();
    }
  }, [isActive]);

  useEffect(() => {
    const fetchUsers = async () => {
      if (activeSection !== 'all' || !isActive) return;
      
      try {
        setIsLoadingMore(true);
        const usersResponse = await userService.getUsers(page, 20);
        const transformedUsers = usersResponse.map(user => ({
          ...user,
          isOnline: false,
          createdAt: new Date(user.createdAt),
          lastLogin: new Date(user.lastLogin)
        }));
        setAllUsers(prev => page === 1 ? transformedUsers : [...prev, ...transformedUsers]);
        setHasMore(usersResponse.length === 20);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке пользователей');
      } finally {
        setIsLoadingMore(false);
      }
    };

    fetchUsers();
  }, [page, activeSection, isActive]);

  const handleAcceptFriend = async (userId: number) => {
    try {
      await userService.acceptFriendRequest(userId);
      const friendsList = await userService.getFriendsList();
      setFriends(friendsList.friends);
      setPendingRequests(friendsList.pendingRequests);

      // Отправляем уведомление о принятии заявки
      const currentUser = JSON.parse(localStorage.getItem('user') || '{}');
      await notificationService.createNotification(
        userId,
        NOTIFICATION_TYPES.FRIEND_ACCEPTED,
        'Заявка в друзья принята',
        `${currentUser.username} принял(а) вашу заявку в друзья`,
        `/profile/${currentUser.username}`
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при принятии заявки');
    }
  };

  const handleDeclineFriend = async (userId: number) => {
    try {
      await userService.declineFriendRequest(userId);
      setPendingRequests(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отклонении заявки');
    }
  };

  const handleRemoveFriend = async (userId: number) => {
    try {
      await userService.removeFriend(userId);
      setFriends(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при удалении из друзей');
    }
  };

  const handleCancelRequest = async (userId: number) => {
    try {
      await userService.declineFriendRequest(userId);
      setSentRequests(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отмене заявки');
    }
  };

  const handleSendFriendRequest = async (userId: number) => {
    try {
      await userService.sendFriendRequest(userId);
      // Обновляем список отправленных заявок
      const friendsList = await userService.getFriendsList();
      setSentRequests(friendsList.sentRequests);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отправке заявки');
    }
  };

  if (!isActive) return null;

  return (
    <div className="tab active">
      <div className="tab-content friends-tab">
        <div className="friends-header">
          <h2 className="tab-title">Друзья</h2>
          <div className="search-container">
            <input
              type="text"
              placeholder="Поиск..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />
          </div>
          <div className="friends-tabs">
            <button 
              className={`tab-button ${activeSection === 'friends' ? 'active' : ''}`}
              onClick={() => {
                setActiveSection('friends');
                setPage(1);
              }}
            >
              Все друзья <span className="count">({friends.length})</span>
            </button>
            <button 
              className={`tab-button ${activeSection === 'pending' ? 'active' : ''}`}
              onClick={() => {
                setActiveSection('pending');
                setPage(1);
              }}
            >
              Входящие <span className="count">({pendingRequests.length})</span>
            </button>
            <button 
              className={`tab-button ${activeSection === 'sent' ? 'active' : ''}`}
              onClick={() => {
                setActiveSection('sent');
                setPage(1);
              }}
            >
              Исходящие <span className="count">({sentRequests.length})</span>
            </button>
            <button 
              className={`tab-button ${activeSection === 'all' ? 'active' : ''}`}
              onClick={() => {
                setActiveSection('all');
                setPage(1);
                setAllUsers([]);
              }}
            >
              Все пользователи
            </button>
          </div>
        </div>

        {error && <div className="error-message">{error}</div>}

        {isLoading ? (
          <div className="loading-message">Загрузка...</div>
        ) : (
          <div className="friends-list">
            {activeSection === 'friends' && (
              filteredFriends.length > 0 ? (
                filteredFriends.map(friend => (
                  <div key={friend.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${friend.username}`)}>
                      <img 
                        src={getMediaUrl(friend.avatarUrl)} 
                        alt={friend.username} 
                        className="friend-avatar"
                      />
                      <div className="friend-details">
                        <div className="friend-name">{friend.username}</div>
                        <div className="friend-status">{friend.status || 'Нет статуса'}</div>
                      </div>
                    </div>
                    <div className="friend-actions">
                      <button 
                        className="action-button message"
                        onClick={() => navigate(`/messages/${friend.id}`)}
                      >
                        Сообщение
                      </button>
                      <button 
                        className="action-button remove"
                        onClick={() => handleRemoveFriend(friend.id)}
                      >
                        Удалить из друзей
                      </button>
                    </div>
                  </div>
                ))
              ) : searchQuery ? (
                <div className="empty-message">Ничего не найдено</div>
              ) : (
                <div className="empty-message">У вас пока нет друзей</div>
              )
            )}

            {activeSection === 'pending' && (
              filteredPendingRequests.length > 0 ? (
                filteredPendingRequests.map(user => (
                  <div key={user.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${user.username}`)}>
                      <img 
                        src={getMediaUrl(user.avatarUrl)} 
                        alt={user.username} 
                        className="friend-avatar"
                      />
                      <div className="friend-details">
                        <div className="friend-name">{user.username}</div>
                        <div className="friend-status">{user.status || 'Нет статуса'}</div>
                      </div>
                    </div>
                    <div className="friend-actions">
                      <button 
                        className="action-button accept"
                        onClick={() => handleAcceptFriend(user.id)}
                      >
                        Принять
                      </button>
                      <button 
                        className="action-button decline"
                        onClick={() => handleDeclineFriend(user.id)}
                      >
                        Отклонить
                      </button>
                    </div>
                  </div>
                ))
              ) : searchQuery ? (
                <div className="empty-message">Ничего не найдено</div>
              ) : (
                <div className="empty-message">Нет входящих заявок</div>
              )
            )}

            {activeSection === 'sent' && (
              filteredSentRequests.length > 0 ? (
                filteredSentRequests.map(user => (
                  <div key={user.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${user.username}`)}>
                      <img 
                        src={getMediaUrl(user.avatarUrl)} 
                        alt={user.username} 
                        className="friend-avatar"
                      />
                      <div className="friend-details">
                        <div className="friend-name">{user.username}</div>
                        <div className="friend-status">{user.status || 'Нет статуса'}</div>
                      </div>
                    </div>
                    <div className="friend-actions">
                      <button 
                        className="action-button cancel"
                        onClick={() => handleCancelRequest(user.id)}
                      >
                        Отменить заявку
                      </button>
                    </div>
                  </div>
                ))
              ) : searchQuery ? (
                <div className="empty-message">Ничего не найдено</div>
              ) : (
                <div className="empty-message">Нет исходящих заявок</div>
              )
            )}

            {activeSection === 'all' && (
              filteredAllUsers.length > 0 ? (
                <>
                  {filteredAllUsers.map((user, index) => (
                    <div 
                      key={user.id} 
                      className="friend-item"
                      ref={index === filteredAllUsers.length - 1 ? lastUserElementRef : null}
                    >
                      <div className="friend-info" onClick={() => navigate(`/profile/${user.username}`)}>
                        <img 
                          src={getMediaUrl(user.avatarUrl)} 
                          alt={user.username} 
                          className="friend-avatar"
                        />
                        <div className="friend-details">
                          <div className="friend-name">{user.username}</div>
                          <div className="friend-status">{user.status || 'Нет статуса'}</div>
                        </div>
                      </div>
                      <div className="friend-actions">
                        {!friends.some(friend => friend.id === user.id) && 
                         !sentRequests.some(request => request.id === user.id) && 
                         !pendingRequests.some(request => request.id === user.id) && 
                         user.id !== currentUserId && (
                          <button 
                            className="action-button add"
                            onClick={() => handleSendFriendRequest(user.id)}
                          >
                            Добавить в друзья
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                  {isLoadingMore && (
                    <div className="loading-message">Загрузка...</div>
                  )}
                </>
              ) : searchQuery ? (
                <div className="empty-message">Ничего не найдено</div>
              ) : (
                <div className="empty-message">Нет пользователей</div>
              )
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default FriendsTab; 