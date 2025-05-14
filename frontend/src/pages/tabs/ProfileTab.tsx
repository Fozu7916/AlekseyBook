import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './Tabs.css';
import { TabProps } from './types';
import { userService, User } from '../../services/userService';
import { postService, WallPost, Comment } from '../../services/postService';
import { onlineStatusService } from '../../services/onlineStatusService';
import './ProfileTab.css';
import { logger } from '../../services/loggerService';
import { getMediaUrl } from '../../config/api.config';

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
  const [activeComments, setActiveComments] = useState<number | null>(null);
  const [commentText, setCommentText] = useState('');
  const [postComments, setPostComments] = useState<{[key: number]: Comment[]}>({});

  const [activeCommentMenu, setActiveCommentMenu] = useState<number | null>(null);
  const [replyingTo, setReplyingTo] = useState<number | null>(null);
  const [replyText, setReplyText] = useState('');
  const [editingComment, setEditingComment] = useState<Comment | null>(null);

  useEffect(() => {
    let unsubscribe: (() => void) | undefined;
    let isConnectionEstablished = false;

    const connectToOnlineStatus = async () => {
      try {
        await onlineStatusService.connect();
        isConnectionEstablished = true;
        unsubscribe = onlineStatusService.onStatusChanged((userId, isOnline, lastLogin) => {
          logger.error('Получено обновление статуса в ProfileTab:', { userId, isOnline, lastLogin });
          setUser(prevUser => {
            if (prevUser && prevUser.id === userId) {
              logger.error('Обновление статуса пользователя:', { 
                prevIsOnline: prevUser.isOnline, 
                newIsOnline: isOnline,
                prevLastLogin: prevUser.lastLogin,
                newLastLogin: lastLogin
              });
              return {
                ...prevUser,
                isOnline,
                lastLogin
              };
            }
            return prevUser;
          });
        });
      } catch (err) {
        logger.error('Ошибка при подключении к сервису онлайн-статуса:', err);
      }
    };

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

        // Если соединение уже установлено, обновляем статус
        if (isConnectionEstablished) {
          await onlineStatusService.updateFocusState(document.hasFocus());
        }

        try {
          const userPosts = await postService.getUserPosts(userData.id);
          
          const postsWithComments = await Promise.all(userPosts.map(async (post) => {
            const comments = await postService.getPostComments(post.id);
            return {
              ...post,
              comments: comments.length
            };
          }));
          
          setPosts(postsWithComments);
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

    // Сначала устанавливаем соединение, затем загружаем профиль
    connectToOnlineStatus().then(() => {
      fetchUserProfile();
    });

    return () => {
      if (unsubscribe) {
        unsubscribe();
      }
      onlineStatusService.disconnect();
    };
  }, [username]);

  const formatLastSeen = (user: User) => {
    if (!user.lastLogin) {
      return 'Не был в сети';
    }
    
    if (user.isOnline) {
      return 'онлайн';
    }

    const lastLogin = new Date(user.lastLogin + 'Z'); // Добавляем 'Z' для явного указания UTC
    const now = new Date();
    const diff = now.getTime() - lastLogin.getTime();
    
    // Меньше минуты
    if (diff < 60000) {
      return 'был только что';
    }
    
    // Меньше часа
    if (diff < 3600000) {
      const minutes = Math.floor(diff / 60000);
      return `был ${minutes} ${minutes === 1 ? 'минуту' : minutes < 5 ? 'минуты' : 'минут'} назад`;
    }
    
    // Сегодня
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (lastLogin >= today) {
      return `был сегодня в ${lastLogin.toLocaleTimeString('ru-RU', { 
        hour: '2-digit', 
        minute: '2-digit',
        hour12: false 
      })}`;
    }
    
    // Вчера
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    if (lastLogin >= yesterday) {
      return `был вчера в ${lastLogin.toLocaleTimeString('ru-RU', { 
        hour: '2-digit', 
        minute: '2-digit',
        hour12: false
      })}`;
    }
    
    // Более старая дата
    return `был ${lastLogin.toLocaleDateString('ru-RU', { 
      day: 'numeric',
      month: 'long',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    })}`;
  };

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

  const handleSaveProfile = async () => {
    try {
      if (!user) return;
      
      const updatedUser = await userService.updateUser(user.id, {
        status: editForm.status,
        bio: editForm.bio
      });
      
      setUser(updatedUser);
      setIsEditing(false);
      setError(null);
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

  const handleSavePost = async () => {
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

  const handleCommentClick = async (postId: number) => {
    if (activeComments === postId) {
      setActiveComments(null);
      return;
    }

    try {
      const comments = await postService.getPostComments(postId);
      setPostComments(prev => ({
        ...prev,
        [postId]: comments
      }));
      setActiveComments(postId);

      setPosts(prevPosts => prevPosts.map(post => 
        post.id === postId 
          ? { ...post, comments: comments.length }
          : post
      ));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при загрузке комментариев');
    }
  };

  const handleAddComment = async (postId: number) => {
    if (!commentText.trim()) return;

    try {
      const newComment = await postService.addComment(postId, commentText.trim());
      
      setPostComments(prev => ({
        ...prev,
        [postId]: [...prev[postId], newComment]
      }));
      
      setPosts(prevPosts => prevPosts.map(post => 
        post.id === postId 
          ? { ...post, comments: post.comments + 1 }
          : post
      ));
      
      setCommentText('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при добавлении комментария');
    }
  };

  const handleCommentLike = async (commentId: number) => {
    try {
      await postService.toggleCommentLike(commentId);
      
      // Оптимистичное обновление
      setPostComments(prev => {
        const updatedComments: { [key: string]: Comment[] } = { ...prev };
        Object.keys(updatedComments).forEach((postId: string) => {
          updatedComments[postId] = updatedComments[postId].map((comment: Comment) => 
            comment.id === commentId 
              ? { ...comment, isLiked: !comment.isLiked, likes: comment.isLiked ? comment.likes - 1 : comment.likes + 1 }
              : comment
          );
        });
        return updatedComments;
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при обработке лайка комментария');
    }
  };

  const handleReplyClick = (commentId: number, authorUsername: string) => {
    setReplyingTo(replyingTo === commentId ? null : commentId);
    setReplyText(replyingTo === commentId ? '' : `[${authorUsername}], `);
  };

  const handleReplySubmit = async (postId: number, parentCommentId: number) => {
    if (!replyText.trim()) return;

    try {
      const newComment = await postService.replyToComment(postId, parentCommentId, replyText.trim());
      
      setPostComments(prev => ({
        ...prev,
        [postId]: [...prev[postId], newComment]
      }));
      
      setPosts(prevPosts => prevPosts.map(post => 
        post.id === postId 
          ? { ...post, comments: post.comments + 1 }
          : post
      ));
      
      setReplyingTo(null);
      setReplyText('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при добавлении ответа');
    }
  };

  const handleCommentMenuClick = (commentId: number) => {
    setActiveCommentMenu(activeCommentMenu === commentId ? null : commentId);
  };

  const handleEditComment = (comment: Comment) => {
    setEditingComment(comment);
    setActiveCommentMenu(null);
  };

  const handleDeleteComment = async (postId: number, commentId: number) => {
    try {
      // Оптимистичное обновление UI
      setPostComments(prev => ({
        ...prev,
        [postId]: prev[postId].filter(comment => comment.id !== commentId)
      }));
      
      // Обновляем количество комментариев в посте
      setPosts(prevPosts => prevPosts.map(post => 
        post.id === postId 
          ? { ...post, comments: post.comments - 1 }
          : post
      ));
      
      // Закрываем меню
      setActiveCommentMenu(null);
      
      // Отправляем запрос на удаление
      await postService.deleteComment(commentId);
    } catch (err) {
      // В случае ошибки возвращаем комментарий обратно
      const error = err instanceof Error ? err.message : 'Ошибка при удалении комментария';
      setError(error);
      
      // Получаем актуальные комментарии с сервера
      try {
        const comments = await postService.getPostComments(postId);
        setPostComments(prev => ({
          ...prev,
          [postId]: comments
        }));
        
        // Обновляем количество комментариев в посте
        setPosts(prevPosts => prevPosts.map(post => 
          post.id === postId 
            ? { ...post, comments: comments.length }
            : post
        ));
      } catch (refreshError) {
        logger.error('Ошибка при обновлении комментариев', refreshError);
      }
    }
  };

  const handleSaveComment = async (postId: number) => {
    if (!editingComment) return;

    try {
      const updatedComment = await postService.updateComment(editingComment.id, editingComment.content);
      
      setPostComments(prev => ({
        ...prev,
        [postId]: prev[postId].map(comment => 
          comment.id === editingComment.id ? updatedComment : comment
        )
      }));
      
      setEditingComment(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при обновлении комментария');
    }
  };

  const canEditComment = (comment: Comment) => {
    return comment.author.id === currentUser?.id;
  };

  const canDeleteComment = (comment: Comment) => {
    return comment.author.id === currentUser?.id || user?.id === currentUser?.id;
  };

  const formatCommentText = (text: string) => {
    return text.replace(/\[([^\]]+)\]/g, (match, username) => {
      return `<a href="#" onclick="event.preventDefault(); window.location.href='/profile/${username}'" class="mention">@${username}</a>`;
    });
  };

  const formatDateTime = (utcDate: string | Date) => {
    let date: Date;
    
    if (utcDate instanceof Date) {
      // Если это локальная дата (при создании поста)
      date = utcDate;
    } else {
      // Если это строка с UTC временем с сервера
      // Проверяем, содержит ли строка 'Z' в конце
      if (utcDate.endsWith('Z')) {
        date = new Date(utcDate);
      } else {
        date = new Date(utcDate + 'Z');
      }
    }
    
    return date.toLocaleString('ru-RU', {
      day: 'numeric',
      month: 'long',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
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
            src={getMediaUrl(user.avatarUrl)} 
            alt={user.username} 
          />
          {user.isOnline && <div className="online-status-indicator" />}
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
                <button className="save-button" onClick={handleSaveProfile}>
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
              <div className={`profile-last-seen ${user.isOnline ? 'online' : ''}`}>
                {formatLastSeen(user)}
              </div>
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
                      src={getMediaUrl(post.authorAvatarUrl)} 
                      alt={post.authorName} 
                      className="post-avatar"
                    />
                    <div className="post-meta">
                      <div className="post-author">{post.authorName}</div>
                      <div className="post-date">
                        {formatDateTime(post.createdAt)}
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
                    <button 
                      className={`post-action ${activeComments === post.id ? 'active' : ''}`}
                      onClick={() => handleCommentClick(post.id)}
                    >
                      <span className="action-icon">💬</span>
                      {post.comments}
                    </button>
                  </div>
                  {activeComments === post.id && (
                    <div className="post-comments">
                      <div className="comments-list">
                        {postComments[post.id]
                          ?.slice()
                          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
                          .map((comment: Comment) => (
                          <div key={comment.id} className="comment">
                            <div 
                              className="comment-avatar-container"
                              onClick={() => navigate(`/profile/${comment.author.username}`)}
                            >
                              <img 
                                src={getMediaUrl(comment.author.avatarUrl)} 
                                alt={comment.author.username} 
                                className="comment-avatar"
                              />
                            </div>
                            <div className="comment-content">
                              <div className="comment-menu">
                                <button 
                                  className="comment-menu-button"
                                  onClick={() => handleCommentMenuClick(comment.id)}
                                >
                                  <div className="comment-menu-dots">
                                    <div className="comment-menu-dot" />
                                    <div className="comment-menu-dot" />
                                    <div className="comment-menu-dot" />
                                  </div>
                                </button>
                                {activeCommentMenu === comment.id && (
                                  <div className="comment-menu-content">
                                    {canEditComment(comment) && (
                                      <div 
                                        className="comment-menu-item"
                                        onClick={() => handleEditComment(comment)}
                                      >
                                        ✏️ Редактировать
                                      </div>
                                    )}
                                    {canDeleteComment(comment) && (
                                      <div 
                                        className="comment-menu-item delete"
                                        onClick={() => handleDeleteComment(post.id, comment.id)}
                                      >
                                        🗑️ Удалить
                                      </div>
                                    )}
                                  </div>
                                )}
                              </div>
                              <div 
                                className="comment-author"
                                onClick={() => navigate(`/profile/${comment.author.username}`)}
                              >
                                {comment.author.username}
                              </div>
                              {editingComment?.id === comment.id ? (
                                <div className="reply-form">
                                  <textarea
                                    value={editingComment.content}
                                    onChange={(e) => setEditingComment({
                                      ...editingComment,
                                      content: e.target.value
                                    })}
                                    placeholder="Редактировать комментарий..."
                                  />
                                  <div className="reply-form-buttons">
                                    <button 
                                      className="cancel-button"
                                      onClick={() => setEditingComment(null)}
                                    >
                                      Отмена
                                    </button>
                                    <button 
                                      className="save-button"
                                      onClick={() => handleSaveComment(post.id)}
                                      disabled={!editingComment.content.trim()}
                                    >
                                      Сохранить
                                    </button>
                                  </div>
                                </div>
                              ) : (
                                <div 
                                  className="comment-text"
                                  dangerouslySetInnerHTML={{ __html: formatCommentText(comment.content) }}
                                />
                              )}
                              <div className="comment-date">
                                {formatDateTime(comment.createdAt)}
                              </div>
                              <div className="comment-actions">
                                <button 
                                  className={`comment-action ${comment.isLiked ? 'liked' : ''}`}
                                  onClick={() => handleCommentLike(comment.id)}
                                >
                                  ❤️ {comment.likes || 0}
                                </button>
                                <button 
                                  className="comment-action reply"
                                  onClick={() => handleReplyClick(comment.id, comment.author.username)}
                                >
                                  💬 Ответить
                                </button>
                              </div>
                              {replyingTo === comment.id && (
                                <div className="reply-form">
                                  <textarea
                                    value={replyText}
                                    onChange={(e) => setReplyText(e.target.value)}
                                    placeholder="Написать ответ..."
                                  />
                                  <div className="reply-form-buttons">
                                    <button 
                                      className="cancel-button"
                                      onClick={() => setReplyingTo(null)}
                                    >
                                      Отмена
                                    </button>
                                    <button 
                                      className="save-button"
                                      onClick={() => handleReplySubmit(post.id, comment.id)}
                                      disabled={!replyText.trim()}
                                    >
                                      Ответить
                                    </button>
                                  </div>
                                </div>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                      <div className="add-comment">
                        <textarea
                          placeholder="Написать комментарий..."
                          value={commentText}
                          onChange={(e) => setCommentText(e.target.value)}
                          maxLength={500}
                        />
                        <button 
                          className="comment-submit-button"
                          onClick={() => handleAddComment(post.id)}
                          disabled={!commentText.trim()}
                        >
                          Отправить
                        </button>
                      </div>
                    </div>
                  )}
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
                      src={getMediaUrl(friend.avatarUrl)} 
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
                onClick={handleSavePost}
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