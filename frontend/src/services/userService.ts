import { chatService } from './chatService';
import { logger } from './loggerService';

export interface User {
  id: number;
  username: string;
  email: string;
  avatarUrl?: string;
  status: string;
  bio?: string;
  isVerified: boolean;
  createdAt: string;
  lastLogin?: string;
}

interface LoginData {
  email: string;
  password: string;
}

interface RegisterData {
  username: string;
  email: string;
  password: string;
  avatarUrl?: string;
  bio?: string;
}

interface UpdateUserData {
  status?: string;
  bio?: string;
}

interface FriendResponse {
  id: number;
  user: User;
  friend: User;
  status: string;
  createdAt: string;
  updatedAt: string;
}

interface FriendList {
  friends: User[];
  pendingRequests: User[];
  sentRequests: User[];
}

interface Message {
  id: number;
  sender: User;
  receiver: User;
  content: string;
  isRead: boolean;
  createdAt: string;
}

interface ChatPreview {
  user: User;
  lastMessage: Message;
  unreadCount: number;
}

class UserService {
  private baseUrl = 'http://localhost:5038/api';
  private token: string | null = null;

  constructor() {
    this.token = localStorage.getItem('token');
  }

  private async request(endpoint: string, options: RequestInit = {}) {
    try {
      const url = `${this.baseUrl}${endpoint}`;
      
      options.headers = {
        'Content-Type': 'application/json',
        ...options.headers,
      };

      if (this.token) {
        options.headers = {
          ...options.headers,
          'Authorization': `Bearer ${this.token}`
        };
      }

      const response = await fetch(url, options);

      if (!response.ok) {
        try {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Ошибка сервера');
        } catch (jsonError) {
          throw new Error(`Ошибка сервера: ${response.status} ${response.statusText}`);
        }
      }

      try {
        return await response.json();
      } catch (jsonError) {
        logger.error('Ошибка при обработке ответа сервера', jsonError);
        throw new Error('Ошибка при обработке ответа сервера');
      }
    } catch (error) {
      logger.error('Ошибка запроса:', error);
      if (error instanceof TypeError && error.message.includes('Failed to fetch')) {
        throw new Error('Ошибка сети. Пожалуйста, проверьте подключение к интернету и убедитесь, что сервер запущен.');
      }
      throw error instanceof Error ? error : new Error('Неизвестная ошибка');
    }
  }

  async login(data: LoginData): Promise<{ token: string; user: User }> {
    try {
      const response = await this.request('/auth/login', {
        method: 'POST',
        body: JSON.stringify(data)
      });

      this.token = response.token;
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));

      return response;
    } catch (error) {
      logger.error('Ошибка авторизации:', error);
      throw error;
    }
  }

  async register(data: RegisterData): Promise<{ token: string; user: User }> {
    try {
      const response = await this.request('/auth/register', {
        method: 'POST',
        body: JSON.stringify({
          username: data.username,
          email: data.email,
          password: data.password,
          avatarUrl: data.avatarUrl,
          bio: data.bio
        })
      });

      this.token = response.token;
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));

      return response;
    } catch (error) {
      logger.error('Ошибка при регистрации', error);
      throw error;
    }
  }

  async getCurrentUser(): Promise<User | null> {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      return JSON.parse(savedUser);
    }
    return null;
  }

  async getUserByUsername(username: string): Promise<User> {
    try {
      return await this.request(`/users/username/${username}`);
    } catch (error) {
      logger.error('Ошибка при получении пользователя по имени', error);
      throw error;
    }
  }

  logout() {
    this.token = null;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  async updateAvatar(file: File): Promise<User> {
    try {
      const currentUser = await this.getCurrentUser();
      if (!currentUser) {
        throw new Error('Пользователь не авторизован');
      }

      const formData = new FormData();
      formData.append('avatar', file);

      const response = await fetch(`${this.baseUrl}/users/${currentUser.id}/avatar`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        },
        body: formData
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при загрузке аватара';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      let updatedUser: User;
      try {
        updatedUser = await response.json();
      } catch (e) {
        // Если сервер не вернул JSON, получаем текущего пользователя
        if (!currentUser) {
          throw new Error('Не удалось получить данные пользователя');
        }
        updatedUser = currentUser;
      }

      localStorage.setItem('user', JSON.stringify(updatedUser));
      return updatedUser;
    } catch (error) {
      logger.error('Ошибка при обновлении аватара', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при загрузке аватара');
    }
  }

  async updateUser(userId: number, data: UpdateUserData): Promise<User> {
    try {
      const response = await this.request(`/users/${userId}`, {
        method: 'PUT',
        body: JSON.stringify(data)
      });

      return response;
    } catch (error) {
      logger.error('Ошибка при обновлении профиля', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при обновлении профиля');
    }
  }

  async sendFriendRequest(friendId: number): Promise<FriendResponse> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${friendId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при отправке запроса в друзья';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      logger.error('Ошибка при отправке запроса в друзья', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при отправке запроса в друзья');
    }
  }

  async acceptFriendRequest(friendId: number): Promise<FriendResponse> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${friendId}/accept`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при принятии запроса в друзья';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      logger.error('Ошибка при принятии запроса в друзья', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при принятии запроса в друзья');
    }
  }

  async declineFriendRequest(friendId: number): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${friendId}/decline`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при отклонении запроса в друзья';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      logger.error('Ошибка при отклонении запроса в друзья', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при отклонении запроса в друзья');
    }
  }

  async removeFriend(friendId: number): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${friendId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при удалении из друзей';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      logger.error('Ошибка при удалении из друзей', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при удалении из друзей');
    }
  }

  async blockUser(userId: number): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${userId}/block`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при блокировке пользователя';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      logger.error('Ошибка при блокировке пользователя', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при блокировке пользователя');
    }
  }

  async getFriendsList(): Promise<FriendList> {
    try {
      const response = await fetch(`${this.baseUrl}/friends`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при получении списка друзей';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      logger.error('Ошибка при получении списка друзей', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении списка друзей');
    }
  }

  async getUserFriendsList(userId: number): Promise<User[]> {
    try {
      const response = await fetch(`${this.baseUrl}/users/${userId}/friends`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        throw new Error('Ошибка при получении списка друзей пользователя');
      }

      return await response.json();
    } catch (error) {
      logger.error('Ошибка при получении списка друзей пользователя', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении списка друзей пользователя');
    }
  }

  async checkFriendshipStatus(friendId: number): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${friendId}/status`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при проверке статуса дружбы';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      logger.error('Ошибка при проверке статуса дружбы', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при проверке статуса дружбы');
    }
  }

  async getUserById(userId: number): Promise<User> {
    return this.request(`/users/${userId}`);
  }

  async sendMessage(receiverId: number, content: string): Promise<Message> {
    try {
      const response = await fetch(`${this.baseUrl}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ receiverId, content })
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Ошибка при отправке сообщения');
      }

      const message = await response.json();
      
      if (chatService.isConnected()) {
        await chatService.sendMessage(message);
      }

      return message;
    } catch (error) {
      logger.error('Ошибка при отправке сообщения:', error);
      throw error;
    }
  }

  async getChatMessages(otherUserId: number): Promise<Message[]> {
    return this.request(`/messages/chat/${otherUserId}`);
  }

  async getUserChats(): Promise<ChatPreview[]> {
    return this.request('/messages/chats');
  }

  async markMessagesAsRead(otherUserId: number): Promise<void> {
    return this.request(`/messages/read/${otherUserId}`, {
      method: 'POST'
    });
  }

  async getUnreadMessagesCount(): Promise<number> {
    return this.request('/messages/unread/count');
  }
}

export const userService = new UserService();
export type { LoginData, RegisterData, UpdateUserData, FriendResponse, FriendList, Message, ChatPreview }; 