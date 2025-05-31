import { HubConnection, HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { Notification } from '../types/notification';
import { API_CONFIG } from '../config/api.config';

type NotificationCallback = (notification: Notification) => void;
type NotificationReadCallback = (notificationId: number) => void;
type AllNotificationsReadCallback = () => void;
type NotificationDeletedCallback = (notificationId: number) => void;

class NotificationHubService {
    private connection: HubConnection | null = null;
    private notificationCallbacks: NotificationCallback[] = [];
    private notificationReadCallbacks: NotificationReadCallback[] = [];
    private allNotificationsReadCallbacks: AllNotificationsReadCallback[] = [];
    private notificationDeletedCallbacks: NotificationDeletedCallback[] = [];
    private isConnecting: boolean = false;
    private reconnectTimeout: NodeJS.Timeout | null = null;
    private readonly maxReconnectAttempts = 5;
    private reconnectAttempts = 0;
    private readonly reconnectDelay = 1000;

    async connect(): Promise<void> {
        if (this.connection?.state === 'Connected' || this.isConnecting) {
            return;
        }

        this.isConnecting = true;
        const token = localStorage.getItem('token');

        if (!token) {
            console.error('Нет токена для подключения к NotificationHub');
            this.isConnecting = false;
            return;
        }

        try {
            this.connection = new HubConnectionBuilder()
                .withUrl(`${API_CONFIG.BASE_URL}/hubs/notification`, {
                    accessTokenFactory: () => token,
                    transport: HttpTransportType.WebSockets,
                    skipNegotiation: true
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        if (retryContext.previousRetryCount === 0) {
                            return 0;
                        } else if (retryContext.previousRetryCount < 3) {
                            return 2000;
                        } else {
                            return 5000;
                        }
                    }
                })
                .configureLogging(LogLevel.Debug)
                .build();

            this.setupConnectionHandlers();
            if (!this.isConnecting) {
                return;
            }
            await this.connection.start();
            console.log('Подключено к NotificationHub');
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error('Ошибка подключения к NotificationHub:', error);
            this.handleReconnect();
        } finally {
            this.isConnecting = false;
        }
    }

    private setupConnectionHandlers() {
        if (!this.connection) return;

        this.connection.onreconnecting((error) => {
            console.warn('Переподключение к NotificationHub...', error);
        });

        this.connection.onreconnected((connectionId) => {
            console.log('Переподключено к NotificationHub', connectionId);
            this.reconnectAttempts = 0;
        });

        this.connection.onclose((error) => {
            console.error('Соединение с NotificationHub закрыто', error);
            this.handleReconnect();
        });

        this.connection.on('ReceiveNotification', (notification: Notification) => {
            console.log('Получено новое уведомление:', notification);
            this.notificationCallbacks.forEach(callback => callback(notification));
        });

        this.connection.on('NotificationRead', (notificationId: number) => {
            console.log('Уведомление отмечено как прочитанное:', notificationId);
            this.notificationReadCallbacks.forEach(callback => callback(notificationId));
        });

        this.connection.on('AllNotificationsRead', () => {
            console.log('Все уведомления отмечены как прочитанные');
            this.allNotificationsReadCallbacks.forEach(callback => callback());
        });

        this.connection.on('NotificationDeleted', (notificationId: number) => {
            console.log('Уведомление удалено:', notificationId);
            this.notificationDeletedCallbacks.forEach(callback => callback(notificationId));
        });
    }

    private handleReconnect() {
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
        }

        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts);
            this.reconnectTimeout = setTimeout(() => {
                this.reconnectAttempts++;
                this.connect();
            }, delay);
        }
    }

    onNotification(callback: NotificationCallback): () => void {
        this.notificationCallbacks.push(callback);
        return () => {
            this.notificationCallbacks = this.notificationCallbacks.filter(cb => cb !== callback);
        };
    }

    onNotificationRead(callback: NotificationReadCallback): () => void {
        this.notificationReadCallbacks.push(callback);
        return () => {
            this.notificationReadCallbacks = this.notificationReadCallbacks.filter(cb => cb !== callback);
        };
    }

    onAllNotificationsRead(callback: AllNotificationsReadCallback): () => void {
        this.allNotificationsReadCallbacks.push(callback);
        return () => {
            this.allNotificationsReadCallbacks = this.allNotificationsReadCallbacks.filter(cb => cb !== callback);
        };
    }

    onNotificationDeleted(callback: NotificationDeletedCallback): () => void {
        this.notificationDeletedCallbacks.push(callback);
        return () => {
            this.notificationDeletedCallbacks = this.notificationDeletedCallbacks.filter(cb => cb !== callback);
        };
    }

    async disconnect(): Promise<void> {
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
        }

        this.isConnecting = false;

        if (this.connection) {
            try {
                const conn = this.connection;
                this.connection = null;
                await conn.stop();
                console.log('Отключено от NotificationHub');
            } catch (error) {
                console.error('Ошибка при отключении от NotificationHub:', error);
            }
        }
    }

    isConnected(): boolean {
        return this.connection?.state === 'Connected';
    }
}

export const notificationHubService = new NotificationHubService(); 