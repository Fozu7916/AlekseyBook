import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './Tabs.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import { postService, WallPost } from '../../services/postService';
import './ProfileTab.css';
import { logger } from '../../services/loggerService';

interface ProfileTabProps extends TabProps {
  username: string;
}

interface FriendPreview {
  id: number;
  username: string;
  avatarUrl?: string;
  status: string;
  isOnline: boolean;
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

const ProfileTab: React.FC<ProfileTabProps> = ({ isActive, username }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isOwner, setIsOwner] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [isFriend, setIsFriend] = useState(false);
  const [friendRequestSent, setFriendRequestSent] = useState(false);
  const [friendRequestReceived, setFriendRequestReceived] = useState(false);
  const [editForm, setEditForm] = useState({
    status: '',
    bio: ''
  });

  const [friends, setFriends] = useState<User[]>([]);
  const [isLoadingFriends, setIsLoadingFriends] = useState(true);

  const [communities] = useState<CommunityPreview[]>([
    {
      id: 1,
      name: 'Любители рок-музыки',
      avatarUrl: 'https://picsum.photos/100?random=5',
      membersCount: 12500
    },
    {
      id: 2,
      name: 'Программисты',
      avatarUrl: 'https://picsum.photos/100?random=6',
      membersCount: 45200
    },
    {
      id: 3,
      name: 'Киберспорт',
      avatarUrl: 'https://picsum.photos/100?random=7',
      membersCount: 78100
    },
    {
      id: 4,
      name: 'Мемы',
      avatarUrl: 'https://picsum.photos/100?random=8',
      membersCount: 156000
    }
  ]);

  const [musicTracks] = useState<MusicTrack[]>([
  ]);

  const [posts, setPosts] = useState<WallPost[]>([]);
  const [isLoadingPosts, setIsLoadingPosts] = useState(true);
  const [newPostContent, setNewPostContent] = useState("");

  const fileInputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  const [editingPost, setEditingPost] = useState<WallPost | null>(null);
  const [editPostContent, setEditPostContent] = useState('');
  const [activePostMenu, setActivePostMenu] = useState<number | null>(null);

  useEffect(() => {
    const fetchUserProfile = async () => {
      try {
        if (!username) return;
        
        const currentUserData = await userService.getCurrentUser();
        setCurrentUser(currentUserData);
        
        const userData = await userService.getUserByUsername(username);
        setUser(userData);
        setEditForm({
          status: userData.status || '',
          bio: userData.bio || ''
        });
        
        const isOwner = currentUserData?.username === username;
        setIsOwner(isOwner);

        try {
          const userPosts = await postService.getUserPosts(userData.id);
          setPosts(userPosts);
        } catch (err) {
          logger.error('Ошибка при получении постов', err);
          setError('Не удалось загрузить посты');
        } finally {
          setIsLoadingPosts(false);
        }

        try {
          if (isOwner) {
            const friendsList = await userService.getFriendsList();
            setFriends(friendsList.friends);
          } else {
            const userFriends = await userService.getUserFriendsList(userData.id);
            setFriends(userFriends);
            
            const friendsList = await userService.getFriendsList();
            setIsFriend(friendsList.friends.some(friend => friend.id === userData.id));
            setFriendRequestSent(friendsList.sentRequests.some(friend => friend.id === userData.id));
            setFriendRequestReceived(friendsList.pendingRequests.some(friend => friend.id === userData.id));
          }
        } catch (err) {
          logger.error('Ошибка при получении списка друзей', err);
          setError('Не удалось загрузить список друзей');
        } finally {
          setIsLoadingFriends(false);
        }
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
    if (!editingPost) return;

    try {
      const updatedPost = await postService.updatePost(editingPost.id, editPostContent);
      setPosts(prevPosts => prevPosts.map(post => 
        post.id === editingPost.id ? { ...updatedPost, isLiked: post.isLiked } : post
      ));
      setEditingPost(null);
      setEditPostContent('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при обновлении поста');
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setEditForm(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handlePostSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newPostContent.trim() || !currentUser || !user) return;

    try {
      const newPost = await postService.createPost(newPostContent.trim(), user.id);
      setPosts(prevPosts => [newPost, ...prevPosts]);
      setNewPostContent('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при создании поста');
    }
  };

  const handleAddFriend = async () => {
    try {
      if (!user) return;
      await userService.sendFriendRequest(user.id);
      setFriendRequestSent(true);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при добавлении в друзья');
    }
  };

  const handleAcceptFriend = async () => {
    try {
      if (!user) return;
      await userService.acceptFriendRequest(user.id);
      setFriendRequestReceived(false);
      setIsFriend(true);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при принятии запроса в друзья');
    }
  };

  const handleDeclineFriend = async () => {
    try {
      if (!user) return;
      await userService.declineFriendRequest(user.id);
      setFriendRequestReceived(false);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при отклонении запроса в друзья');
    }
  };

  const handleRemoveFriend = async () => {
    try {
      if (!user) return;
      await userService.removeFriend(user.id);
      setIsFriend(false);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при удалении из друзей');
    }
  };

  const handleSendMessage = () => {
    if (user) {
      navigate(`/messages/${user.id}`);
    }
  };

  const handleLikeClick = async (postId: number) => {
    try {
      setPosts(prevPosts => prevPosts.map(post => {
        if (post.id === postId) {
          return {
            ...post,
            isLiked: !post.isLiked,
            likes: post.isLiked ? post.likes - 1 : post.likes + 1
          };
        }
        return post;
      }));

      await postService.toggleLike(postId);
      
      const updatedLikes = await postService.getPostLikes(postId);
      
      setPosts(prevPosts => prevPosts.map(post => {
        if (post.id === postId) {
          const currentUser = JSON.parse(localStorage.getItem('user') || '{}');
          const isLikedByCurrentUser = updatedLikes.some(like => like.user.id === currentUser?.id);
          
          return {
            ...post,
            likes: updatedLikes.length,
            isLiked: isLikedByCurrentUser
          };
        }
        return post;
      }));
    } catch (err) {
      // В случае ошибки возвращаем предыдущее состояние
      const errorMessage = err instanceof Error ? err.message : 'Ошибка при обработке лайка';
      setError(errorMessage);
      
      // Откатываем оптимистичное обновление
      setPosts(prevPosts => prevPosts.map(post => {
        if (post.id === postId) {
          return {
            ...post,
            isLiked: !post.isLiked,
            likes: post.isLiked ? post.likes + 1 : post.likes - 1
          };
        }
        return post;
      }));

      if (errorMessage.includes('авторизац')) {
        // Тут можно добавить редирект на страницу логина
        console.log('Необходима авторизация');
      }
    }
  };

  const handlePostMenuClick = (postId: number) => {
    setActivePostMenu(activePostMenu === postId ? null : postId);
  };

  const handleEditPost = (post: WallPost) => {
    setEditingPost(post);
    setEditPostContent(post.content);
    setActivePostMenu(null);
  };

  const handleDeletePost = async (postId: number) => {
    if (!window.confirm('Вы уверены, что хотите удалить этот пост?')) return;

    try {
      await postService.deletePost(postId);
      setPosts(prevPosts => prevPosts.filter(post => post.id !== postId));
      setActivePostMenu(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при удалении поста');
    }
  };

  const canEditPost = (post: WallPost) => {
    return post.authorId === currentUser?.id;
  };

  const canDeletePost = (post: WallPost) => {
    return post.authorId === currentUser?.id || user?.id === currentUser?.id;
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
            src={user.avatarUrl ? `http://localhost:5038${user.avatarUrl}` : '/images/default-avatar.svg'} 
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
              <div className="profile-bio">{user.bio}</div>
              {user.lastLogin && (
                <div className="profile-last-seen">
                  <span>Последний вход:</span>
                  <span>{new Date(user.lastLogin).toLocaleDateString()}</span>
                </div>
              )}
              {isOwner ? (
                <button className="edit-profile-button" onClick={handleEditClick}>
                  Редактировать профиль
                </button>
              ) : currentUser ? (
                <div className="profile-actions">
                  {!isFriend && !friendRequestSent && !friendRequestReceived && (
                    <button className="friend-button" onClick={handleAddFriend}>
                      Добавить в друзья
                    </button>
                  )}
                  {friendRequestSent && (
                    <button className="friend-button sent" disabled>
                      Запрос отправлен
                    </button>
                  )}
                  {friendRequestReceived && (
                    <div className="friend-request-actions">
                      <button className="friend-button accept" onClick={handleAcceptFriend}>
                        Принять запрос
                      </button>
                      <button className="friend-button decline" onClick={handleDeclineFriend}>
                        Отклонить
                      </button>
                    </div>
                  )}
                  {isFriend && (
                    <button className="friend-button remove" onClick={handleRemoveFriend}>
                      Удалить из друзей
                    </button>
                  )}
                  <button className="message-button" onClick={handleSendMessage}>
                    Написать сообщение
                  </button>
                </div>
              ) : null}
            </>
          )}
        </div>
      </div>

      <div className="profile-main">
        <div className="wall-section">
          <form className="post-form" onSubmit={handlePostSubmit}>
            <textarea
              placeholder={isOwner ? "Что у вас нового?" : `Написать на стене ${user.username}...`}
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

          <div className="posts-list">
            {isLoadingPosts ? (
              <div className="loading-posts">Загрузка постов...</div>
            ) : posts.length > 0 ? (
              posts.map(post => (
                <div key={post.id} className="post-card">
                  <div className="post-header">
                    <img 
                      src={post.authorAvatarUrl ? `http://localhost:5038${post.authorAvatarUrl}` : '/images/default-avatar.svg'} 
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
                    <div className="post-menu">
                      <button 
                        className="post-menu-button"
                        onClick={() => handlePostMenuClick(post.id)}
                      >
                        <div className="post-menu-dots">
                          <div className="post-menu-dot" />
                          <div className="post-menu-dot" />
                          <div className="post-menu-dot" />
                        </div>
                      </button>
                      {activePostMenu === post.id && (
                        <div className="post-menu-content">
                          {canEditPost(post) && (
                            <div 
                              className="post-menu-item"
                              onClick={() => handleEditPost(post)}
                            >
                              ✏️ Редактировать
                            </div>
                          )}
                          {canDeletePost(post) && (
                            <div 
                              className="post-menu-item delete"
                              onClick={() => handleDeletePost(post.id)}
                            >
                              🗑️ Удалить
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="post-content">{post.content}</div>
                  <div className="post-footer">
                    <button 
                      className={`post-action ${post.isLiked ? 'liked' : ''}`} 
                      onClick={() => handleLikeClick(post.id)}
                    >
                      <span className="action-icon">❤️</span>
                      {post.likes}
                    </button>
                    <button className="post-action">
                      <span className="action-icon">💬</span>
                      {post.comments}
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="no-posts">Нет постов</div>
            )}
          </div>
        </div>
      </div>

      <div className="profile-sidebar">
        <div className="profile-section friends-section">
          <h2>Друзья <span className="count">({friends.length})</span></h2>
          <div className="friends-grid">
            {isLoadingFriends ? (
              <div className="loading-friends">Загрузка друзей...</div>
            ) : friends.length > 0 ? (
              <>
                {friends.slice(0, 3).map(friend => (
                  <div key={friend.id} className="friend-card" onClick={() => navigate(`/profile/${friend.username}`)}>
                    <img 
                      src={friend.avatarUrl ? `http://localhost:5038${friend.avatarUrl}` : '/images/default-avatar.svg'} 
                      alt={friend.username} 
                      className="friend-avatar"
                    />
                    <div className="friend-info">
                      <div className="friend-name">{friend.username}</div>
                      <div className="friend-status">{friend.status || 'Нет статуса'}</div>
                    </div>
                  </div>
                ))}
                {friends.length > 3 && (
                  <div className="view-all-button" onClick={() => navigate('/friends')}>
                    Показать всех
                  </div>
                )}
              </>
            ) : (
              <div className="no-friends">Нет друзей</div>
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
              <div className="view-all-button" onClick={() => navigate('/messages')}>
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
              <div className="view-all-button" onClick={() => navigate('/music')}>
                Показать все
              </div>
            )}
          </div>
        </div>
      </div>

      {editingPost && (
        <>
          <div className="modal-overlay" onClick={() => setEditingPost(null)} />
          <div className="edit-post-modal" onClick={e => e.stopPropagation()}>
            <h3>Редактирование поста</h3>
            <textarea
              value={editPostContent}
              onChange={e => setEditPostContent(e.target.value)}
              placeholder="Введите новый текст поста"
            />
            <div className="edit-post-modal-buttons">
              <button className="cancel-button" onClick={() => setEditingPost(null)}>
                Отмена
              </button>
              <button 
                className="save-button"
                onClick={handleSaveEdit}
                disabled={!editPostContent.trim() || editPostContent === editingPost.content}
              >
                Сохранить
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default ProfileTab; 