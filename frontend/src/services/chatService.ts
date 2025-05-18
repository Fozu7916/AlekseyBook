import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { userService } from './userService';
import { Message } from './userService';
import * as signalR from '@microsoft/signalr';
import { logger } from './loggerService';
import { API_CONFIG } from '../config/api.config';

export interface IChatService {
  startConnection(): Promise<void>;
  stopConnection(): Promise<void>;
  isConnected(): boolean;
  onMessage(callback: (message: Message) => void): () => void;
  onTypingStatus(callback: (userId: string, isTyping: boolean) => void): () => void;
  onMessageStatusUpdate(callback: (messageId: number, senderId: number, receiverId: number, isRead: boolean) => void): () => void;
  sendTypingStatus(receiverId: string, isTyping: boolean): Promise<void>;
}

export class ChatService implements IChatService {
  private connection: HubConnection | null = null;
  private typingCallbacks: ((userId: string, isTyping: boolean) => void)[] = [];
  private messageCallbacks: ((message: Message) => void)[] = [];
  private messageStatusCallbacks: ((messageId: number, senderId: number, receiverId: number, isRead: boolean) => void)[] = [];
  private connectionPromise: Promise<void> | null = null;
  private isConnecting: boolean = false;
  private shouldStop: boolean = false;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private lastReconnectAttempt: number = 0;
  private readonly MIN_RECONNECT_INTERVAL = 5000; // 5 секунд

  public async startConnection(): Promise<void> {
    const token = localStorage.getItem('token');
    if (!token) {
      return;
    }

    if (this.isConnecting) {
      await this.connectionPromise;
      return;
    }

    if (this.connection?.state === 'Connected') {
      return;
    }

    this.shouldStop = false;
    this.isConnecting = true;
    
    try {
      await this.initializeConnection();
    } finally {
      this.isConnecting = false;
    }
  }

  private async initializeConnection() {
    try {
      const token = localStorage.getItem('token');
      if (!token) {
        return;
      }

      if (this.connection) {
        try {
          await this.connection.stop();
          this.connection = null;
        } catch (err) {
          logger.error('Ошибка при закрытии предыдущего подключения', err);
        }
      }

      if (this.shouldStop) {
        return;
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(API_CONFIG.CHAT_HUB_URL, {
          accessTokenFactory: () => localStorage.getItem('token') || '',
          transport: signalR.HttpTransportType.WebSockets,
          skipNegotiation: true,
          logMessageContent: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            const now = Date.now();
            if (now - this.lastReconnectAttempt < this.MIN_RECONNECT_INTERVAL) {
              return this.MIN_RECONNECT_INTERVAL;
            }
            this.lastReconnectAttempt = now;
            
            if (retryContext.previousRetryCount === 0) {
              return 0;
            }
            if (retryContext.previousRetryCount < 3) {
              return 2000;
            }
            if (retryContext.previousRetryCount < 5) {
              return 5000;
            }
            if (retryContext.previousRetryCount < 10) {
              return 10000;
            }
            return 30000;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.setupHandlers();

      if (this.shouldStop) {
        return;
      }

      await this.connection.start();
      logger.error('SignalR соединение установлено');

      if (this.shouldStop) {
        await this.connection.stop();
        this.connection = null;
        return;
      }

      const currentUser = await userService.getCurrentUser();
      if (currentUser && this.connection && this.connection.state === 'Connected') {
        await this.connection.invoke('JoinChat', currentUser.id.toString());
        logger.error('Успешно присоединились к чату');
      }
    } catch (err) {
      logger.error('Ошибка подключения SignalR', err);
      if (!this.shouldStop) {
        this.scheduleReconnect();
      }
      throw err;
    }
  }

  private scheduleReconnect() {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
    }

    this.reconnectTimer = setTimeout(async () => {
      if (!this.isConnected() && !this.shouldStop && !this.isConnecting) {
        logger.error('Попытка переподключения к SignalR...');
        try {
          await this.startConnection();
        } catch (err) {
          logger.error('Ошибка при попытке переподключения', err);
          this.scheduleReconnect();
        }
      }
    }, this.MIN_RECONNECT_INTERVAL);
  }

  private setupHandlers() {
    if (!this.connection) return;

    this.connection.onreconnecting(() => {
      logger.error('SignalR пытается переподключиться...');
    });

    this.connection.onreconnected(async () => {
      logger.error('SignalR успешно переподключился');
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser && this.connection?.state === 'Connected') {
          await this.connection.invoke('JoinChat', currentUser.id.toString());
          logger.error('Успешно переприсоединились к чату после переподключения');
        }
      } catch (err) {
        logger.error('Ошибка при переприсоединении к чату', err);
      }
    });

    this.connection.onclose((error) => {
      logger.error('SignalR соединение закрыто', error);
      if (!this.shouldStop) {
        this.scheduleReconnect();
      }
    });

    this.connection.on('ReceiveMessage', (message: Message) => {
      this.messageCallbacks.forEach(callback => {
        try {
          callback(message);
        } catch (err) {
          logger.error('Ошибка в обработчике сообщения:', err);
        }
      });
    });

    this.connection.on('TypingStatus', (userId: string, isTyping: boolean) => {
      this.typingCallbacks.forEach(callback => {
        try {
          callback(userId, isTyping);
        } catch (err) {
          logger.error('Ошибка в обработчике статуса печатания:', err);
        }
      });
    });

    this.connection.on('MessageStatusUpdate', (messageId: number, senderId: number, receiverId: number, isRead: boolean) => {
      this.messageStatusCallbacks.forEach(callback => {
        try {
          callback(messageId, senderId, receiverId, isRead);
        } catch (err) {
          logger.error('Ошибка в обработчике статуса сообщения:', err);
        }
      });
    });
  }

  public async stopConnection() {
    this.shouldStop = true;

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.connection) {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser && this.connection.state === 'Connected') {
          await this.connection.invoke('LeaveChat', currentUser.id.toString());
        }
      } catch (err) {
        logger.error('Ошибка при выходе из чата', err);
      }

      try {
        await this.connection.stop();
        logger.error('SignalR соединение успешно закрыто');
      } catch (err) {
        logger.error('Ошибка при отключении SignalR', err);
      } finally {
        this.connection = null;
      }
    }
  }

  public async sendMessage(message: Message) {
    let retryCount = 0;
    const maxRetries = 3;

    while (retryCount < maxRetries) {
      try {
        if (!this.isConnected()) {
          await this.startConnection();
          await new Promise(resolve => setTimeout(resolve, 1000));
        }

        if (!this.connection || this.connection.state !== 'Connected') {
          throw new Error('Соединение не установлено');
        }

        await this.connection.invoke('SendMessage', {
          content: message.content,
          receiverId: message.receiver.id
        });
        return;
      } catch (err) {
        logger.error(`Ошибка отправки сообщения через SignalR (попытка ${retryCount + 1}/${maxRetries})`, err);
        retryCount++;
        if (retryCount < maxRetries) {
          await new Promise(resolve => setTimeout(resolve, 1000 * retryCount));
        }
      }
    }
    throw new Error('Не удалось отправить сообщение после нескольких попыток');
  }

  public isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  public onTypingStatus(callback: (userId: string, isTyping: boolean) => void) {
    this.typingCallbacks.push(callback);
    return () => {
      this.typingCallbacks = this.typingCallbacks.filter(cb => cb !== callback);
    };
  }

  public onMessage(callback: (message: Message) => void) {
    this.messageCallbacks.push(callback);
    return () => {
      this.messageCallbacks = this.messageCallbacks.filter(cb => cb !== callback);
    };
  }

  public async sendTypingStatus(receiverId: string, isTyping: boolean) {
    if (!this.connection || this.connection.state !== 'Connected') {
      return;
    }

    try {
      await this.connection.invoke('SendTypingStatus', receiverId, isTyping);
    } catch (err) {
      logger.error('Ошибка при отправке статуса печатания', err);
    }
  }

  public onMessageStatusUpdate(callback: (messageId: number, senderId: number, receiverId: number, isRead: boolean) => void) {
    this.messageStatusCallbacks.push(callback);
    return () => {
      this.messageStatusCallbacks = this.messageStatusCallbacks.filter(cb => cb !== callback);
    };
  }
}

export const chatService: IChatService = new ChatService(); 