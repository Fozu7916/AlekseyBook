import React, { useState, useEffect } from 'react';
import './Tabs.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import './ProfileTab.css';

interface ProfileTabProps extends TabProps {
  username: string;
}

const ProfileTab: React.FC<ProfileTabProps> = ({ isActive, username }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchUserProfile = async () => {
      try {
        if (!username) return;
        const userData = await userService.getUserByUsername(username);
        setUser(userData);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке профиля');
      } finally {
        setIsLoading(false);
      }
    };

    fetchUserProfile();
  }, [username]);

  if (!isActive) return null;

  if (isLoading) {
    return <div className="profile-loading">Загрузка профиля...</div>;
  }

  if (error) {
    return <div className="profile-error">{error}</div>;
  }

  if (!user) {
    return <div className="profile-not-found">Пользователь не найден</div>;
  }

  return (
    <div className="profile-container">
      <div className="profile-header">
        <div className="profile-avatar">
          <img src={user.avatarUrl || '/default-avatar.png'} alt={user.username} />
        </div>
        <div className="profile-info">
          <h1>{user.username}</h1>
          <div className="profile-status">{user.status}</div>
          {user.bio && <div className="profile-bio">{user.bio}</div>}
        </div>
      </div>
      <div className="profile-details">
        <div className="profile-stat">
          <label>Дата регистрации:</label>
          <span>{new Date(user.createdAt).toLocaleDateString()}</span>
        </div>
        {user.lastLogin && (
          <div className="profile-stat">
            <label>Последний вход:</label>
            <span>{new Date(user.lastLogin).toLocaleDateString()}</span>
          </div>
        )}
      </div>
    </div>
  );
};

export default ProfileTab; 