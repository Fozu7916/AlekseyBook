import { userService } from './userService';
import config from '../config';

export interface Message {
  id: number;
  senderId: number;
  receiverId: number;
  content: string;
  createdAt: string;
  isRead: boolean;
}

export interface Chat {
  userId: number;
  username: string;
  avatarUrl?: string;
  lastMessage?: Message;
  unreadCount: number;
}

export class MessageService {
  private baseUrl = config.apiUrl;
  private token: string | null = null;

  constructor() {
    this.token = localStorage.getItem('token');
  }

  private async request(endpoint: string, options: RequestInit = {}) {
    if (!this.token) {
      throw new Error('Не авторизован');
    }

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
      } catch {}
      throw new Error(errorMessage);
    }

    return response.json();
  }

  async getChats(): Promise<Chat[]> {
    return this.request('/messages/chats');
  }

  async getMessages(userId: number): Promise<Message[]> {
    return this.request(`/messages/${userId}`);
  }

  async sendMessage(userId: number, content: string): Promise<Message> {
    return this.request(`/messages/${userId}`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    });
  }

  async markAsRead(userId: number): Promise<void> {
    await this.request(`/messages/${userId}/read`, {
      method: 'POST',
    });
  }

  async deleteMessage(messageId: number): Promise<void> {
    await this.request(`/messages/${messageId}`, {
      method: 'DELETE',
    });
  }
}

export const messageService = new MessageService(); 