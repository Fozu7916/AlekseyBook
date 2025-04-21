import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { userService } from './userService';
import { Message } from './userService';
import * as signalR from '@microsoft/signalr';

export class ChatService {
  private connection: HubConnection | null = null;
  private typingCallbacks: ((userId: string, isTyping: boolean) => void)[] = [];
  private messageCallbacks: ((message: Message) => void)[] = [];
  private connectionPromise: Promise<void> | null = null;
  private isConnecting: boolean = false;
  private shouldStop: boolean = false;

  public async startConnection() {
    // Проверяем наличие токена
    const token = localStorage.getItem('token');
    if (!token) {
      console.log('Нет токена авторизации, подключение невозможно');
      return;
    }

    if (this.isConnecting) {
      console.log('Подключение уже в процессе');
      return this.connectionPromise;
    }

    if (this.connection?.state === 'Connected') {
      console.log('SignalR уже подключен');
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
      // Проверяем наличие токена
      const token = localStorage.getItem('token');
      if (!token) {
        console.log('Нет токена авторизации, подключение невозможно');
        return;
      }

      // Если есть активное подключение, сначала корректно закрываем его
      if (this.connection) {
        try {
          await this.connection.stop();
          this.connection = null;
        } catch (err) {
          console.warn('Ошибка при закрытии предыдущего подключения:', err);
        }
      }

      if (this.shouldStop) {
        console.log('Остановка подключения запрошена');
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

      // Добавляем обработчики до подключения
      this.setupHandlers();

      if (this.shouldStop) {
        console.log('Остановка подключения запрошена');
        return;
      }

      await this.connection.start();
      console.log('SignalR успешно подключен');

      if (this.shouldStop) {
        await this.connection.stop();
        this.connection = null;
        console.log('Подключение остановлено по запросу');
        return;
      }

      // После подключения присоединяемся к чату
      const currentUser = await userService.getCurrentUser();
      if (currentUser && this.connection && this.connection.state === 'Connected') {
        await this.connection.invoke('JoinChat', currentUser.id.toString());
        console.log('Присоединились к чату как пользователь:', currentUser.id);
      }
    } catch (err) {
      console.error('Ошибка подключения SignalR:', err);
      if (!this.shouldStop) {
        // Планируем переподключение только если не было запроса на остановку
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
    console.log('Запрошена остановка подключения');
    this.shouldStop = true;

    if (this.connection) {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser && this.connection.state === 'Connected') {
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
      } finally {
        this.connection = null;
        this.isConnecting = false;
        this.connectionPromise = null;
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
    return this.connection?.state === 'Connected' && !this.shouldStop;
  }

  public async sendMessage(message: Message) {
    let retryCount = 0;
    const maxRetries = 3;

    while (retryCount < maxRetries) {
      try {
        if (!this.isConnected()) {
          console.log('SignalR не подключен, пробуем переподключиться...');
          await this.startConnection();
          // Ждем немного после переподключения
          await new Promise(resolve => setTimeout(resolve, 1000));
        }

        if (!this.connection || this.connection.state !== 'Connected') {
          throw new Error('Соединение не установлено');
        }

        console.log('Отправка сообщения через SignalR:', message);
        await this.connection.invoke('SendMessage', {
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

    // Обработчик статуса печатания
    this.connection.on('ReceiveTypingStatus', (userId: string, isTyping: boolean) => {
      console.log('Получен статус печатания от сервера:', userId, isTyping);
      this.typingCallbacks.forEach(callback => {
        try {
          callback(userId, isTyping);
        } catch (err) {
          console.error('Ошибка в обработчике статуса печатания:', err);
        }
      });
    });

    // Обработчик новых сообщений
    this.connection.on('ReceiveMessage', (message: Message) => {
      console.log('Получено новое сообщение от сервера:', message);
      this.messageCallbacks.forEach(callback => {
        try {
          callback(message);
        } catch (err) {
          console.error('Ошибка в обработчике сообщений:', err);
        }
      });
    });

    // Обработчики состояния подключения
    this.connection.onreconnecting(() => {
      console.log('SignalR переподключается...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR переподключен');
    });

    this.connection.onclose(() => {
      console.log('SignalR закрыт');
    });
  }
}

export const chatService = new ChatService(); 