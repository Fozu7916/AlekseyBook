import { logger } from './loggerService';

export interface WallPost {
  id: number;
  authorId: number;
  authorName: string;
  authorAvatar?: string;
  content: string;
  createdAt: Date;
  likes: number;
  comments: number;
}

interface CreateWallPostDto {
  content: string;
  imageUrl?: string;
  wallOwnerId: number;
}

class PostService {
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

  async getUserPosts(userId: number): Promise<WallPost[]> {
    try {
      const response = await this.request(`/wall-posts/user/${userId}`);
      return response.map((post: any) => ({
        ...post,
        createdAt: new Date(post.createdAt)
      }));
    } catch (error) {
      logger.error('Ошибка при получении постов пользователя', error);
      throw error;
    }
  }

  async createPost(content: string, wallOwnerId: number): Promise<WallPost> {
    try {
      const postData: CreateWallPostDto = {
        content,
        wallOwnerId
      };

      const response = await this.request('/wall-posts', {
        method: 'POST',
        body: JSON.stringify(postData)
      });
      return {
        ...response,
        createdAt: new Date(response.createdAt)
      };
    } catch (error) {
      logger.error('Ошибка при создании поста', error);
      throw error;
    }
  }

  async deletePost(postId: number): Promise<void> {
    try {
      await this.request(`/wall-posts/${postId}`, {
        method: 'DELETE'
      });
    } catch (error) {
      logger.error('Ошибка при удалении поста', error);
      throw error;
    }
  }
}

export const postService = new PostService(); 