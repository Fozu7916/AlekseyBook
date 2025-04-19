interface User {
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
      console.log('Making request to:', url);
      
      // Добавляем заголовки по умолчанию
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

      console.log('Request options:', {
        ...options,
        body: options.body ? JSON.parse(options.body as string) : undefined
      });

      const response = await fetch(url, options);
      console.log('Response status:', response.status);

      if (!response.ok) {
        try {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Ошибка сервера');
        } catch (jsonError) {
          throw new Error(`Ошибка сервера: ${response.status} ${response.statusText}`);
        }
      }

      try {
        const data = await response.json();
        console.log('Response data:', data);
        return data;
      } catch (jsonError) {
        console.error('Error parsing response:', jsonError);
        throw new Error('Ошибка при обработке ответа сервера');
      }
    } catch (error) {
      console.error('Request error:', error);
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
      console.error('Login error:', error);
      throw error;
    }
  }

  async register(data: RegisterData): Promise<{ token: string; user: User }> {
    try {
      const response = await this.request('/users', {
        method: 'POST',
        body: JSON.stringify(data)
      });

      this.token = response.token;
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));

      return response;
    } catch (error) {
      console.error('Register error:', error);
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
      console.error('Get user by username error:', error);
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
      console.error('Update avatar error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при загрузке аватара');
    }
  }

  async updateUser(userId: number, data: UpdateUserData): Promise<User> {
    try {
      console.log('Updating user:', { userId, data });
      
      const response = await this.request(`/users/${userId}`, {
        method: 'PUT',
        body: JSON.stringify(data)
      });

      console.log('Updated user:', response);
      return response;
    } catch (error) {
      console.error('Update user error:', error);
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
      console.error('Send friend request error:', error);
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
      console.error('Accept friend request error:', error);
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
      console.error('Decline friend request error:', error);
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
      console.error('Remove friend error:', error);
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
      console.error('Block user error:', error);
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
      console.error('Get friends list error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении списка друзей');
    }
  }

  async getUserFriendsList(userId: number): Promise<User[]> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/user/${userId}`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при получении списка друзей пользователя';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      console.error('Get user friends list error:', error);
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
      console.error('Check friendship status error:', error);
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
          'Authorization': `Bearer ${this.token}`
        },
        body: JSON.stringify({ receiverId, content })
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при отправке сообщения';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      console.error('Send message error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при отправке сообщения');
    }
  }

  async getChatMessages(otherUserId: number): Promise<Message[]> {
    try {
      const response = await fetch(`${this.baseUrl}/messages/chat/${otherUserId}`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при получении сообщений';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      console.error('Get chat messages error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении сообщений');
    }
  }

  async getUserChats(): Promise<ChatPreview[]> {
    try {
      const response = await fetch(`${this.baseUrl}/messages/chats`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при получении списка чатов';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      console.error('Get user chats error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении списка чатов');
    }
  }

  async markMessagesAsRead(otherUserId: number): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/messages/read/${otherUserId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при отметке сообщений как прочитанных';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      console.error('Mark messages as read error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при отметке сообщений как прочитанных');
    }
  }

  async getUnreadMessagesCount(): Promise<number> {
    try {
      const response = await fetch(`${this.baseUrl}/messages/unread/count`, {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при получении количества непрочитанных сообщений';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      console.error('Get unread messages count error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при получении количества непрочитанных сообщений');
    }
  }
}

export const userService = new UserService();
export type { User, LoginData, RegisterData, UpdateUserData, FriendResponse, FriendList, Message, ChatPreview }; 