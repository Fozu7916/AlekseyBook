import { logger } from './loggerService';
import { API_CONFIG } from '../config/api.config';

export interface WallPost {
  id: number;
  authorId: number;
  authorName: string;
  authorAvatarUrl?: string;
  content: string;
  createdAt: Date;
  likes: number;
  comments: number;
  isLiked?: boolean;
}

export interface LikeDto {
  id: number;
  user: {
    id: number;
    username: string;
    email: string;
    avatarUrl?: string;
  };
  createdAt: string;
}

export interface UpdatePostDto {
  content: string;
}

interface CreateWallPostDto {
  content: string;
  imageUrl?: string;
  wallOwnerId: number;
}

export interface Comment {
  id: number;
  content: string;
  author: {
    id: number;
    username: string;
    avatarUrl?: string;
  };
  createdAt: string;
  updatedAt: string;
  likes: number;
  isLiked: boolean;
  parentId?: number;
}

class PostService {
  private baseUrl = API_CONFIG.API_URL;
  private token: string | null = null;

  constructor() {
    this.updateToken();
  }

  private updateToken() {
    const token = localStorage.getItem('token');
    if (!token) {
      logger.error('Токен авторизации не найден');
    }
    this.token = token;
  }

  private async request(endpoint: string, options: RequestInit = {}) {
    this.updateToken();

    if (!endpoint) {
      throw new Error('Endpoint не может быть пустым');
    }

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

      if (options.method === 'DELETE' || response.status === 204) {
        return undefined;
      }

      try {
        return await response.json();
      } catch (jsonError) {
        if (response.status === 200 && response.headers.get('content-length') === '0') {
          return undefined;
        }
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
      const posts = response.map((post: any) => ({
        ...post,
        createdAt: post.createdAt.endsWith('Z') 
          ? new Date(post.createdAt)
          : new Date(post.createdAt + 'Z')
      }));

      const postsWithLikes = await Promise.all(
        posts.map(async (post: WallPost) => {
          try {
            const likes = await this.getPostLikes(post.id);
            const currentUser = JSON.parse(localStorage.getItem('user') || '{}');
            const currentUserId = currentUser?.id;
            
            return {
              ...post,
              likes: likes.length,
              isLiked: likes.some(like => like.user?.id === currentUserId)
            };
          } catch (error) {
            logger.error(`Ошибка при получении лайков для поста ${post.id}`, error);
            return {
              ...post,
              likes: 0,
              isLiked: false
            };
          }
        })
      );

      return postsWithLikes;
    } catch (error) {
      logger.error('Ошибка при получении постов пользователя', error);
      throw error;
    }
  }

  async createPost(content: string, wallOwnerId: number): Promise<WallPost> {
    try {
      const postData = {
        content,
        wallOwnerId
      };

      const response = await this.request('/wall-posts', {
        method: 'POST',
        body: JSON.stringify(postData)
      });
      
      // Добавляем 'Z' к строке даты, чтобы указать, что это UTC
      const createdAt = response.createdAt.endsWith('Z') 
        ? new Date(response.createdAt)
        : new Date(response.createdAt + 'Z');

      return {
        ...response,
        createdAt,
        isLiked: false
      };
    } catch (error) {
      logger.error('Ошибка при создании поста', error);
      throw error;
    }
  }

  async updatePost(postId: number, content: string): Promise<WallPost> {
    try {
      const response = await this.request(`/wall-posts/${postId}`, {
        method: 'PUT',
        body: JSON.stringify({ content })
      });
      return {
        ...response,
        createdAt: response.createdAt.endsWith('Z') 
          ? new Date(response.createdAt)
          : new Date(response.createdAt + 'Z')
      };
    } catch (error) {
      logger.error('Ошибка при обновлении поста', error);
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

  async getPostLikes(postId: number): Promise<LikeDto[]> {
    try {
      return await this.request(`/LikeComment/posts/${postId}/likes`);
    } catch (error) {
      logger.error('Ошибка при получении лайков поста', error);
      throw error;
    }
  }

  async toggleLike(postId: number): Promise<LikeDto | { message: string }> {
    if (!this.token) {
      logger.error('Попытка поставить лайк без авторизации');
      throw new Error('Необходима авторизация');
    }

    try {
      const response = await this.request(`/LikeComment/posts/${postId}/likes`, {
        method: 'POST'
      });

      if (response && 'message' in response) {
        return response as { message: string };
      }
      return response as LikeDto;
    } catch (error) {
      logger.error('Ошибка при переключении лайка', error);
      throw error;
    }
  }

  async getPostComments(postId: number): Promise<Comment[]> {
    try {
      return await this.request(`/LikeComment/posts/${postId}/comments`, {
        method: 'GET'
      });
    } catch (error) {
      logger.error('Ошибка при получении комментариев', error);
      throw error;
    }
  }

  async addComment(postId: number, content: string): Promise<Comment> {
    try {
      return await this.request(`/LikeComment/posts/${postId}/comments`, {
        method: 'POST',
        body: JSON.stringify({ content, wallPostId: postId })
      });
    } catch (error) {
      logger.error('Ошибка при добавлении комментария', error);
      throw error;
    }
  }

  async toggleCommentLike(commentId: number): Promise<void> {
    try {
      await this.request(`/LikeComment/comments/${commentId}/like`, {
        method: 'POST'
      });
    } catch (error) {
      logger.error('Ошибка при обработке лайка комментария', error);
      throw error;
    }
  }

  async replyToComment(postId: number, parentCommentId: number, content: string): Promise<Comment> {
    try {
      return await this.request(`/LikeComment/posts/${postId}/comments`, {
        method: 'POST',
        body: JSON.stringify({ content, wallPostId: postId, parentId: parentCommentId })
      });
    } catch (error) {
      logger.error('Ошибка при ответе на комментарий', error);
      throw error;
    }
  }

  async updateComment(commentId: number, content: string): Promise<Comment> {
    try {
      return await this.request(`/LikeComment/comments/${commentId}`, {
        method: 'PUT',
        body: JSON.stringify({ content })
      });
    } catch (error) {
      logger.error('Ошибка при обновлении комментария', error);
      throw error;
    }
  }

  async deleteComment(commentId: number): Promise<void> {
    try {
      await this.request(`/LikeComment/comments/${commentId}`, {
        method: 'DELETE'
      });
    } catch (error) {
      logger.error('Ошибка при удалении комментария', error);
      throw error;
    }
  }
}

export const postService = new PostService(); 