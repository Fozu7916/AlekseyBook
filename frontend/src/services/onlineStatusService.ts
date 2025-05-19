import { HubConnection, HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import { logger } from './loggerService';
import { API_CONFIG } from '../config/api.config';

interface OnlineStatusCallback {
  (userId: number, isOnline: boolean, lastLogin: Date): void;
}

class OnlineStatusService {
  private connection: HubConnection | null = null;
  private callbacks: OnlineStatusCallback[] = [];
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private connectionPromise: Promise<void> | null = null;
  private isConnecting = false;

  public getConnectionStatus(): boolean {
    return this.isConnected && this.connection?.state === 'Connected';
  }

  async connect() {
    if (this.isConnecting) {
      await this.connectionPromise;
      return;
    }

    if (this.connection?.state === 'Connected') {
      return;
    }

    this.isConnecting = true;

    try {
      const token = localStorage.getItem('token');
      if (!token) {
        throw new Error('Не найден токен авторизации');
      }

      if (this.connection) {
        await this.disconnect();
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(`${API_CONFIG.ONLINE_STATUS_HUB_URL}?access_token=${token}`, {
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build();

      this.connection.on('UserOnlineStatusChanged', (data: { userId: number, isOnline: boolean, lastLogin: string }) => {
        logger.error('Получено обновление статуса:', data);
        this.callbacks.forEach(callback => {
          try {
            callback(data.userId, data.isOnline, new Date(data.lastLogin));
          } catch (err) {
            logger.error('Ошибка в обработчике обновления статуса:', err);
          }
        });
      });

      this.connectionPromise = this.connection.start();
      await this.connectionPromise;
      
      this.isConnected = true;
      this.reconnectAttempts = 0;
      logger.error('Подключение к хабу онлайн-статуса установлено');

      this.connection.onreconnecting(() => {
        logger.error('Переподключение к хабу онлайн-статуса...');
        this.isConnected = false;
      });

      this.connection.onreconnected(async () => {
        logger.error('Переподключение к хабу онлайн-статуса выполнено успешно');
        this.isConnected = true;
        this.reconnectAttempts = 0;
      });

      this.connection.onclose(() => {
        logger.error('Соединение с хабом онлайн-статуса закрыто');
        this.isConnected = false;
        this.scheduleReconnect();
      });

    } catch (err) {
      logger.error('Ошибка при подключении к хабу онлайн-статуса:', err);
      this.isConnected = false;
      this.scheduleReconnect();
      throw err;
    } finally {
      this.isConnecting = false;
      this.connectionPromise = null;
    }
  }

  private scheduleReconnect() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts - 1), 30000);
      setTimeout(() => this.connect(), delay);
    }
  }

  onUserStatusChanged(callback: OnlineStatusCallback) {
    this.callbacks.push(callback);
    return () => {
      this.callbacks = this.callbacks.filter(cb => cb !== callback);
    };
  }

  async disconnect() {
    try {
      if (this.connection) {
        this.isConnected = false;
        await this.connection.stop();
        this.connection = null;
      }
    } catch (err) {
      logger.error('Ошибка при отключении от хаба онлайн-статуса:', err);
    }
  }

  async getOnlineUsers() {
    try {
      if (this.connection?.state === 'Connected' && this.isConnected) {
        await this.connection.invoke('GetOnlineUsers');
      }
    } catch (err) {
      logger.error('Ошибка при получении списка онлайн пользователей:', err);
    }
  }
}

export const onlineStatusService = new OnlineStatusService(); 