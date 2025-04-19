import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { userService } from './userService';
import { Message } from './userService';
import * as signalR from '@microsoft/signalr';

export class ChatService {
  private connection: HubConnection | null = null;
  private typingCallbacks: ((userId: string, isTyping: boolean) => void)[] = [];
  private messageCallbacks: ((message: Message) => void)[] = [];

  public async startConnection() {
    if (this.connection?.state === 'Connected') {
      console.log('SignalR уже подключен');
      return;
    }

    try {
      if (this.connection) {
        await this.connection.stop();
        this.connection = null;
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

      // Добавляем обработчики до подключения
      this.setupHandlers();

      await this.connection.start();
      console.log('SignalR успешно подключен');

      // После подключения присоединяемся к чату
      const currentUser = await userService.getCurrentUser();
      if (currentUser) {
        await this.connection.invoke('JoinChat', currentUser.id.toString());
        console.log('Присоединились к чату как пользователь:', currentUser.id);
      }
    } catch (err) {
      console.error('Ошибка подключения SignalR:', err);
      // Пробуем переподключиться через 3 секунды
      setTimeout(() => this.startConnection(), 3000);
    }
  }

  public async stopConnection() {
    if (this.connection) {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser) {
          await this.connection.invoke('LeaveChat', currentUser.id.toString());
          console.log('Покинули чат как пользователь:', currentUser.id);
        }
      } catch (err) {
        console.error('Ошибка при выходе из чата:', err);
      }

      try {
        await this.connection.stop();
        console.log('SignalR отключен');
      } catch (err) {
        console.error('Ошибка при отключении SignalR:', err);
      }
    }
  }

  public async sendTypingStatus(userId: string, isTyping: boolean) {
    if (this.connection?.state === 'Connected') {
      try {
        await this.connection.invoke('SendTypingStatus', userId, isTyping);
        console.log('Отправлен статус печатания:', userId, isTyping);
      } catch (err) {
        console.error('Error sending typing status:', err);
      }
    } else {
      console.warn('SignalR not connected');
    }
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

  public isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  public async sendMessage(message: Message) {
    let retryCount = 0;
    const maxRetries = 3;

    while (retryCount < maxRetries) {
      try {
        if (!this.isConnected()) {
          console.log('SignalR не подключен, пробуем переподключиться...');
          await this.startConnection();
          await new Promise(resolve => setTimeout(resolve, 1000)); // Ждем 1 секунду после переподключения
        }

        console.log('Отправка сообщения через SignalR:', message);
        await this.connection!.invoke('SendMessage', {
          content: message.content,
          receiverId: message.receiver.id
        });
        console.log('Сообщение успешно отправлено через SignalR');
        return;
      } catch (err) {
        console.error(`Ошибка отправки сообщения через SignalR (попытка ${retryCount + 1}/${maxRetries}):`, err);
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

    this.connection.onreconnecting((error) => {
      console.log('SignalR переподключается...', error);
    });

    this.connection.onreconnected(async (connectionId) => {
      console.log('SignalR переподключен с ID:', connectionId);
      const currentUser = await userService.getCurrentUser();
      if (currentUser) {
        await this.connection?.invoke('JoinChat', currentUser.id.toString());
        console.log('Переприсоединились к чату после переподключения');
      }
    });

    this.connection.onclose((error) => {
      console.log('SignalR соединение закрыто:', error);
      // Пробуем переподключиться
      setTimeout(() => this.startConnection(), 3000);
    });

    this.connection.on('ReceiveMessage', (message: Message) => {
      console.log('Получено сообщение через SignalR:', message);
      this.messageCallbacks.forEach(callback => callback(message));
    });

    this.connection.on('ReceiveTypingStatus', (userId: string, isTyping: boolean) => {
      console.log('Получен статус печатания через SignalR:', userId, isTyping);
      this.typingCallbacks.forEach(callback => callback(userId, isTyping));
    });
  }
}

export const chatService = new ChatService(); 