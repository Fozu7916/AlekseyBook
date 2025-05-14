import config from '../config';
import { Message, User } from './userService';
import { logger } from './loggerService';

interface Chat {
  user: User;
  lastMessage: Message | null;
  unreadCount: number;
}

class MessageService {
  private baseUrl = config.apiUrl;
  private token: string | null = null;

  constructor() {
    this.token = localStorage.getItem('token');
  }

  private async request(endpoint: string, options: RequestInit = {}) {
    if (!this.token) {
      throw new Error('Не авторизован');
    }

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        ...options,
        headers: {
          ...options.headers,
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        let errorMessage = 'Произошла ошибка';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch (err) {
          logger.error('Ошибка при разборе ответа с ошибкой', err);
        }
        throw new Error(errorMessage);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      logger.error('Ошибка при запросе к API сообщений', { endpoint, error });
      throw error;
    }
  }

  async getChats(): Promise<Chat[]> {
    try {
      return await this.request('/messages/chats');
    } catch (error) {
      logger.error('Ошибка при получении списка чатов', error);
      throw error;
    }
  }

  async getMessages(userId: number): Promise<Message[]> {
    try {
      return await this.request(`/messages/chat/${userId}`);
    } catch (error) {
      logger.error('Ошибка при получении сообщений', error);
      throw error;
    }
  }

  async sendMessage(userId: number, content: string): Promise<Message> {
    try {
      return await this.request('/messages', {
        method: 'POST',
        body: JSON.stringify({ receiverId: userId, content }),
      });
    } catch (error) {
      logger.error('Ошибка при отправке сообщения', error);
      throw error;
    }
  }

  async markAsRead(userId: number): Promise<void> {
    try {
      await this.request(`/messages/read/${userId}`, {
        method: 'POST',
      });
    } catch (error) {
      logger.error('Ошибка при отметке сообщений как прочитанных', error);
      throw error;
    }
  }

  async deleteMessage(messageId: number): Promise<void> {
    try {
      await this.request(`/messages/${messageId}`, {
        method: 'DELETE',
      });
    } catch (error) {
      logger.error('Ошибка при удалении сообщения', error);
      throw error;
    }
  }
}

export const messageService = new MessageService(); 