import React, { useState, useEffect, useRef } from 'react';
import './Tabs.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import './ProfileTab.css';

interface ProfileTabProps extends TabProps {
  username: string;
}

interface FriendPreview {
  id: number;
  username: string;
  avatarUrl?: string;
  status: string;
}

interface CommunityPreview {
  id: number;
  name: string;
  avatarUrl?: string;
  membersCount: number;
}

interface MusicTrack {
  id: number;
  title: string;
  artist: string;
  coverUrl?: string;
}

interface WallPost {
  id: number;
  authorId: number;
  authorName: string;
  authorAvatar?: string;
  content: string;
  createdAt: Date;
  likes: number;
  comments: number;
}

const ProfileTab: React.FC<ProfileTabProps> = ({ isActive, username }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isOwner, setIsOwner] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState({
    status: '',
    bio: ''
  });

  const [friends] = useState<FriendPreview[]>([
  ]);

  const [communities] = useState<CommunityPreview[]>([
  ]);

  const [musicTracks] = useState<MusicTrack[]>([
  ]);

  const [posts, setPosts] = useState<WallPost[]>([
    {
      id: 1,
      authorId: 1,
      authorName: "Иван Иванов",
      authorAvatar: "/default-avatar.png",
      content: "Привет всем! Это мой первый пост на стене 👋",
      createdAt: new Date(),
      likes: 5,
      comments: 2
    },
    {
      id: 2,
      authorId: 2,
      authorName: "Мария Петрова",
      authorAvatar: "/default-avatar.png",
      content: "Отличный профиль! Давно не виделись 😊",
      createdAt: new Date(Date.now() - 86400000), // вчера
      likes: 3,
      comments: 1
    }
  ]);
  const [newPostContent, setNewPostContent] = useState("");

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const fetchUserProfile = async () => {
      try {
        if (!username) return;
        const userData = await userService.getUserByUsername(username);
        setUser(userData);
        setEditForm({
          status: userData.status || '',
          bio: userData.bio || ''
        });
        
        const currentUser = await userService.getCurrentUser();
        setIsOwner(currentUser?.username === username);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Ошибка при загрузке профиля');
      } finally {
        setIsLoading(false);
      }
    };

    fetchUserProfile();
  }, [username]);

  const handleAvatarClick = () => {
    if (isOwner) {
      fileInputRef.current?.click();
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      setError('Размер файла не должен превышать 5MB');
      return;
    }

    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      setError('Допустимые форматы: JPG, JPEG, PNG, GIF');
      return;
    }

    try {
      setIsUploading(true);
      setError(null);
      const updatedUser = await userService.updateAvatar(file);
      setUser(updatedUser);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке аватара');
    } finally {
      setIsUploading(false);
    }
  };

  const handleEditClick = () => {
    setIsEditing(true);
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    if (user) {
      setEditForm({
        status: user.status || '',
        bio: user.bio || ''
      });
    }
  };

  const handleSaveEdit = async () => {
    try {
      if (!user) return;

      if (!editForm.status.trim()) {
        setError('Статус обязателен для заполнения');
        return;
      }

      if (editForm.status.length > 50) {
        setError('Статус не может быть длиннее 50 символов');
        return;
      }

      if (editForm.bio && editForm.bio.length > 1000) {
        setError('Биография не может быть длиннее 1000 символов');
        return;
      }

      setError(null);
      const updatedUser = await userService.updateUser(user.id, {
        status: editForm.status.trim(),
        bio: editForm.bio?.trim()
      });
      
      setUser(updatedUser);
      setIsEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при обновлении профиля');
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setEditForm(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handlePostSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newPostContent.trim()) return;

    const newPost: WallPost = {
      id: posts.length + 1,
      authorId: 1, // текущий пользователь
      authorName: "Вы",
      authorAvatar: user?.avatarUrl,
      content: newPostContent.trim(),
      createdAt: new Date(),
      likes: 0,
      comments: 0
    };

    setPosts([newPost, ...posts]);
    setNewPostContent("");
  };

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
        <div 
          className={`profile-avatar ${isOwner ? 'editable' : ''}`} 
          onClick={handleAvatarClick} 
          style={{ cursor: isOwner ? 'pointer' : 'default' }}
        >
          <img 
            src={user.avatarUrl ? `http://localhost:5038${user.avatarUrl}` : '/default-avatar.png'} 
            alt={user.username} 
          />
          {isUploading && <div className="avatar-uploading">Загрузка...</div>}
          {isOwner && <div className="avatar-edit-hint">Нажмите для изменения</div>}
        </div>
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileChange}
          accept="image/*"
          style={{ display: 'none' }}
        />
        <div className="profile-info">
          <h1>{user.username}</h1>
          {isEditing ? (
            <div className="edit-form">
              <div className="form-group">
                <label>Статус:</label>
                <input
                  type="text"
                  name="status"
                  value={editForm.status}
                  onChange={handleInputChange}
                  placeholder="Введите статус"
                />
              </div>
              <div className="form-group">
                <label>О себе:</label>
                <textarea
                  name="bio"
                  value={editForm.bio}
                  onChange={handleInputChange}
                  placeholder="Расскажите о себе"
                  rows={4}
                />
              </div>
              <div className="edit-buttons">
                <button className="save-button" onClick={handleSaveEdit}>
                  Сохранить
                </button>
                <button className="cancel-button" onClick={handleCancelEdit}>
                  Отмена
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="profile-status">{user.status}</div>
              {user.bio && <div className="profile-bio">{user.bio}</div>}
              {user.lastLogin && (
                <div className="profile-last-seen">
                  <span>Последний вход:</span>
                  <span>{new Date(user.lastLogin).toLocaleDateString()}</span>
                </div>
              )}
              {isOwner && (
                <button className="edit-profile-button" onClick={handleEditClick}>
                  Редактировать профиль
                </button>
              )}
            </>
          )}
        </div>
      </div>

      <div className="profile-main">
        <div className="wall-section">
          {isOwner && (
            <form className="post-form" onSubmit={handlePostSubmit}>
              <textarea
                placeholder="Что у вас нового?"
                value={newPostContent}
                onChange={(e) => setNewPostContent(e.target.value)}
                maxLength={1000}
              />
              <div className="post-form-footer">
                <span className="character-count">
                  {newPostContent.length}/1000
                </span>
                <button 
                  type="submit" 
                  disabled={!newPostContent.trim()}
                  className="post-submit-button"
                >
                  Опубликовать
                </button>
              </div>
            </form>
          )}

          <div className="posts-list">
            {posts.map(post => (
              <div key={post.id} className="post-card">
                <div className="post-header">
                  <img 
                    src={post.authorAvatar || '/default-avatar.png'} 
                    alt={post.authorName} 
                    className="post-avatar"
                  />
                  <div className="post-meta">
                    <div className="post-author">{post.authorName}</div>
                    <div className="post-date">
                      {new Date(post.createdAt).toLocaleDateString('ru-RU', {
                        day: 'numeric',
                        month: 'long',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </div>
                  </div>
                </div>
                <div className="post-content">{post.content}</div>
                <div className="post-footer">
                  <button className="post-action">
                    <span className="action-icon">❤️</span>
                    {post.likes}
                  </button>
                  <button className="post-action">
                    <span className="action-icon">💬</span>
                    {post.comments}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="profile-sidebar">
        <div className="profile-section friends-section">
          <h2>Друзья <span className="count">({friends.length})</span></h2>
          <div className="friends-grid">
            {friends.slice(0, 3).map(friend => (
              <div key={friend.id} className="friend-card">
                <img 
                  src={friend.avatarUrl || '/default-avatar.png'} 
                  alt={friend.username} 
                  className="friend-avatar"
                />
                <div className="friend-info">
                  <div className="friend-name">{friend.username}</div>
                  <div className={`friend-status ${friend.status}`}>{friend.status}</div>
                </div>
              </div>
            ))}
            {friends.length > 3 && (
              <div className="view-all-button">
                Показать всех
              </div>
            )}
          </div>
        </div>

        <div className="profile-section communities-section">
          <h2>Сообщества <span className="count">({communities.length})</span></h2>
          <div className="communities-grid">
            {communities.slice(0, 3).map(community => (
              <div key={community.id} className="community-card">
                <img 
                  src={community.avatarUrl || '/default-community.png'} 
                  alt={community.name} 
                  className="community-avatar"
                />
                <div className="community-info">
                  <div className="community-name">{community.name}</div>
                  <div className="members-count">{community.membersCount} участников</div>
                </div>
              </div>
            ))}
            {communities.length > 3 && (
              <div className="view-all-button">
                Показать все
              </div>
            )}
          </div>
        </div>

        <div className="profile-section music-section">
          <h2>Музыка <span className="count">({musicTracks.length})</span></h2>
          <div className="music-list">
            {musicTracks.slice(0, 3).map(track => (
              <div key={track.id} className="track-card">
                <div className="track-cover">
                  <img 
                    src={track.coverUrl || '/default-track.png'} 
                    alt={track.title} 
                  />
                  <button className="play-button">▶</button>
                </div>
                <div className="track-info">
                  <div className="track-title">{track.title}</div>
                  <div className="track-artist">{track.artist}</div>
                </div>
              </div>
            ))}
            {musicTracks.length > 3 && (
              <div className="view-all-button">
                Показать все
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfileTab; 