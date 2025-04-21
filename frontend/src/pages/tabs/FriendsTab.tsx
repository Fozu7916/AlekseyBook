import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './Tabs.css';
import './FriendsTab.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import { Avatar } from '../../components/Avatar';

const FriendsTab: React.FC<TabProps> = ({ isActive }) => {
  const [friends, setFriends] = useState<User[]>([]);
  const [pendingRequests, setPendingRequests] = useState<User[]>([]);
  const [sentRequests, setSentRequests] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<'friends' | 'pending' | 'sent'>('friends');
  
  const navigate = useNavigate();

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

  const handleAcceptFriend = async (userId: number) => {
    try {
      await userService.acceptFriendRequest(userId);
      // Обновляем списки после принятия заявки
      const friendsList = await userService.getFriendsList();
      setFriends(friendsList.friends);
      setPendingRequests(friendsList.pendingRequests);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при принятии заявки');
    }
  };

  const handleDeclineFriend = async (userId: number) => {
    try {
      await userService.declineFriendRequest(userId);
      // Обновляем список входящих заявок
      setPendingRequests(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отклонении заявки');
    }
  };

  const handleRemoveFriend = async (userId: number) => {
    try {
      await userService.removeFriend(userId);
      // Обновляем список друзей
      setFriends(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при удалении из друзей');
    }
  };

  const handleCancelRequest = async (userId: number) => {
    try {
      await userService.declineFriendRequest(userId);
      // Обновляем список исходящих заявок
      setSentRequests(prev => prev.filter(user => user.id !== userId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отмене заявки');
    }
  };

  if (!isActive) return null;

  return (
    <div className="tab active">
      <div className="tab-content friends-tab">
        <div className="friends-header">
          <h2 className="tab-title">Друзья</h2>
          <div className="friends-tabs">
            <button 
              className={`tab-button ${activeSection === 'friends' ? 'active' : ''}`}
              onClick={() => setActiveSection('friends')}
            >
              Все друзья <span className="count">({friends.length})</span>
            </button>
            <button 
              className={`tab-button ${activeSection === 'pending' ? 'active' : ''}`}
              onClick={() => setActiveSection('pending')}
            >
              Входящие <span className="count">({pendingRequests.length})</span>
            </button>
            <button 
              className={`tab-button ${activeSection === 'sent' ? 'active' : ''}`}
              onClick={() => setActiveSection('sent')}
            >
              Исходящие <span className="count">({sentRequests.length})</span>
            </button>
          </div>
        </div>

        {error && <div className="error-message">{error}</div>}

        {isLoading ? (
          <div className="loading-message">Загрузка...</div>
        ) : (
          <div className="friends-list">
            {activeSection === 'friends' && (
              friends.length > 0 ? (
                friends.map(friend => (
                  <div key={friend.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${friend.username}`)}>
                      <Avatar 
                        avatarUrl={friend.avatarUrl}
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
              ) : (
                <div className="empty-message">У вас пока нет друзей</div>
              )
            )}

            {activeSection === 'pending' && (
              pendingRequests.length > 0 ? (
                pendingRequests.map(user => (
                  <div key={user.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${user.username}`)}>
                      <Avatar 
                        avatarUrl={user.avatarUrl}
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
              ) : (
                <div className="empty-message">Нет входящих заявок</div>
              )
            )}

            {activeSection === 'sent' && (
              sentRequests.length > 0 ? (
                sentRequests.map(user => (
                  <div key={user.id} className="friend-item">
                    <div className="friend-info" onClick={() => navigate(`/profile/${user.username}`)}>
                      <Avatar 
                        avatarUrl={user.avatarUrl}
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
              ) : (
                <div className="empty-message">Нет исходящих заявок</div>
              )
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default FriendsTab; 