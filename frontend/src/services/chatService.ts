import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { userService } from './userService';
import { Message } from './userService';
import * as signalR from '@microsoft/signalr';
import { logger } from './loggerService';

export class ChatService {
  private connection: HubConnection | null = null;
  private typingCallbacks: ((userId: string, isTyping: boolean) => void)[] = [];
  private messageCallbacks: ((message: Message) => void)[] = [];
  private connectionPromise: Promise<void> | null = null;
  private isConnecting: boolean = false;
  private shouldStop: boolean = false;

  public async startConnection() {
    const token = localStorage.getItem('token');
    if (!token) {
      return;
    }

    if (this.isConnecting) {
      return this.connectionPromise;
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
        .withUrl('http://localhost:5038/chatHub', {
          accessTokenFactory: () => localStorage.getItem('token') || '',
          transport: signalR.HttpTransportType.WebSockets,
          skipNegotiation: true,
          logMessageContent: true
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Debug)
        .build();

      this.setupHandlers();

      if (this.shouldStop) {
        return;
      }

      await this.connection.start();

      if (this.shouldStop) {
        await this.connection.stop();
        this.connection = null;
        return;
      }

      const currentUser = await userService.getCurrentUser();
      if (currentUser && this.connection && this.connection.state === 'Connected') {
        await this.connection.invoke('JoinChat', currentUser.id.toString());
      }
    } catch (err) {
      logger.error('Ошибка подключения SignalR', err);
      if (!this.shouldStop) {
        setTimeout(() => {
          if (!this.isConnected() && !this.shouldStop) {
            this.startConnection();
          }
        }, 5000);
      }
      throw err;
    }
  }

  public async stopConnection() {
    logger.info('Запрошена остановка подключения');
    this.shouldStop = true;

    if (this.connection) {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser && this.connection.state === 'Connected') {
          await this.connection.invoke('LeaveChat', currentUser.id.toString());
          logger.info('Покинули чат', { userId: currentUser.id });
        }
      } catch (err) {
        logger.error('Ошибка при выходе из чата', err);
      }

      try {
        await this.connection.stop();
        logger.info('SignalR отключен');
      } catch (err) {
        logger.error('Ошибка при отключении SignalR', err);
      } finally {
        this.connection = null;
        this.isConnecting = false;
        this.connectionPromise = null;
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

  private setupHandlers() {
    if (!this.connection) return;

    this.connection.on('ReceiveTypingStatus', (userId: string, isTyping: boolean) => {
      this.typingCallbacks.forEach(callback => {
        try {
          callback(userId, isTyping);
        } catch (err) {
          logger.error('Ошибка в обработчике статуса печатания', err);
        }
      });
    });

    this.connection.on('ReceiveMessage', (message: Message) => {
      this.messageCallbacks.forEach(callback => {
        try {
          callback(message);
        } catch (err) {
          logger.error('Ошибка в обработчике сообщений', err);
        }
      });
    });

    this.connection.onreconnecting(() => {});

    this.connection.onreconnected(async () => {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser && this.connection?.state === 'Connected') {
          await this.connection.invoke('JoinChat', currentUser.id.toString());
        }
      } catch (err) {
        logger.error('Ошибка при переприсоединении к чату', err);
      }
    });

    this.connection.onclose(() => {});
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
}

export const chatService = new ChatService(); 