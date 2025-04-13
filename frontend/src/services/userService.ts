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

      try {
        const response = await fetch(url, options);
        console.log('Response status:', response.status);
        
        const data = await response.json();
        console.log('Response data:', data);
        
        if (!response.ok) {
          throw new Error(data.message || 'Ошибка сервера');
        }

        return data;
      } catch (networkError) {
        console.error('Network error:', networkError);
        throw new Error('Ошибка сети. Пожалуйста, проверьте подключение к интернету и убедитесь, что сервер запущен.');
      }
    } catch (error) {
      console.error('Request error:', error);
      if (error instanceof Error) {
        throw error;
      }
      throw new Error('Неизвестная ошибка');
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

      return response;
    } catch (error) {
      console.error('Register error:', error);
      throw error;
    }
  }

  async getCurrentUser(): Promise<User | null> {
    if (!this.token) return null;

    try {
      return await this.request('/users/me');
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
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
  }
}

export const userService = new UserService();
export type { User, LoginData, RegisterData }; 