import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { logger } from './loggerService';
import config from '../config';

interface OnlineStatusCallback {
  (userId: number, isOnline: boolean, lastLogin: Date): void;
}

class OnlineStatusService {
  private connection: HubConnection | null = null;
  private callbacks: OnlineStatusCallback[] = [];
  private handleFocus = () => this.updateFocusState(true);
  private handleBlur = () => this.updateFocusState(false);
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private lastFocusState: boolean | null = null;

  async connect() {
    try {
      const token = localStorage.getItem('token');
      if (!token) {
        throw new Error('Не найден токен авторизации');
      }

      if (this.connection) {
        await this.disconnect();
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(`${config.baseUrl}/onlineStatusHub`, {
          accessTokenFactory: () => token,
          withCredentials: true
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
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

      await this.connection.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;
      logger.error('Подключение к хабу онлайн-статуса установлено');
      
      window.addEventListener('focus', this.handleFocus);
      window.addEventListener('blur', this.handleBlur);
      
      this.lastFocusState = document.hasFocus();
      await this.updateFocusState(this.lastFocusState);

      this.connection.onreconnecting(() => {
        logger.error('Переподключение к хабу онлайн-статуса...');
        this.isConnected = false;
      });

      this.connection.onreconnected(async () => {
        logger.error('Переподключение к хабу онлайн-статуса выполнено успешно');
        this.isConnected = true;
        this.reconnectAttempts = 0;
        
        if (this.lastFocusState !== null) {
          await this.updateFocusState(this.lastFocusState);
        }
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
    }
  }

  private scheduleReconnect() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      logger.error('Достигнуто максимальное количество попыток переподключения');
      return;
    }

    setTimeout(async () => {
      try {
        if (!this.isConnected) {
          this.reconnectAttempts++;
          logger.error(`Попытка переподключения к хабу онлайн-статуса (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
          await this.connect();
        }
      } catch (err) {
        logger.error('Ошибка при попытке переподключения:', err);
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.scheduleReconnect();
        }
      }
    }, Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000));
  }

  async disconnect() {
    try {
      if (this.connection) {
        window.removeEventListener('focus', this.handleFocus);
        window.removeEventListener('blur', this.handleBlur);
        this.isConnected = false;
        this.lastFocusState = null;
        await this.connection.stop();
        this.connection = null;
      }
    } catch (err) {
      logger.error('Ошибка при отключении от хаба онлайн-статуса:', err);
    }
  }

  async updateFocusState(isFocused: boolean) {
    try {
      if (!this.connection) {
        logger.error('Соединение не инициализировано');
        return;
      }

      if (this.connection.state !== 'Connected') {
        logger.error(`Неверное состояние соединения: ${this.connection.state}`);
        return;
      }

      if (!this.isConnected) {
        logger.error('Соединение помечено как отключенное');
        return;
      }

      this.lastFocusState = isFocused;

      logger.error('Отправка обновления состояния фокуса:', isFocused);
      await this.connection.invoke('UpdateFocusState', isFocused);
    } catch (err) {
      logger.error('Ошибка при обновлении состояния фокуса:', err);
      this.isConnected = false;
      this.scheduleReconnect();
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

  onStatusChanged(callback: OnlineStatusCallback) {
    this.callbacks.push(callback);
    return () => {
      this.callbacks = this.callbacks.filter(cb => cb !== callback);
    };
  }
}

export const onlineStatusService = new OnlineStatusService(); 