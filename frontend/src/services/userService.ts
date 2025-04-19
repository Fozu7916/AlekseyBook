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

  async addFriend(userId: number): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/friends/${userId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        let errorMessage = 'Ошибка при добавлении в друзья';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      console.error('Add friend error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при добавлении в друзья');
    }
  }

  async sendMessage(userId: number, message: string): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/messages`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          recipientId: userId,
          content: message
        })
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
    } catch (error) {
      console.error('Send message error:', error);
      throw error instanceof Error 
        ? error 
        : new Error('Ошибка при отправке сообщения');
    }
  }
}

export const userService = new UserService();
export type { User, LoginData, RegisterData, UpdateUserData }; 